using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Clients;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Exceptions;
using DevOpsProject.Shared.Messages;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.HiveMindCommands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.CommunicationControl.Logic.Services
{
    public class CommunicationControlService : ICommunicationControlService
    {
        private readonly ISpatialService _spatialService;
        private readonly IRedisKeyValueService _redisService;
        private readonly RedisKeys _redisKeys;
        private readonly IPublishService _messageBus;
        private readonly CommunicationControlHttpClient _hiveHttpClient;
        private readonly ILogger<CommunicationControlService> _logger;
        private readonly IOptionsMonitor<ComControlCommunicationConfiguration> _communicationControlConfiguration;

        public CommunicationControlService(ISpatialService spatialService, IRedisKeyValueService redisService, IOptionsSnapshot<RedisKeys> redisKeysSnapshot, 
            IPublishService messageBus, CommunicationControlHttpClient hiveHttpClient, ILogger<CommunicationControlService> logger, IOptionsMonitor<ComControlCommunicationConfiguration> communicationControlConfiguration)
        {
            _spatialService = spatialService;
            _redisService = redisService;
            _redisKeys = redisKeysSnapshot.Value;
            _messageBus = messageBus;
            _hiveHttpClient = hiveHttpClient;
            _logger = logger;
            _communicationControlConfiguration = communicationControlConfiguration;
        }

        public async Task<bool> DisconnectHive(string hiveId)
        {
            bool isSuccessfullyDisconnected = false;
            try
            {
                var result = await _redisService.DeleteAsync(GetHiveKey(hiveId));
                isSuccessfullyDisconnected = result;
                return result;
            }
            finally
            {
                await _messageBus.Publish(new HiveDisconnectedMessage
                {
                    HiveID = hiveId,
                    IsSuccessfullyDisconnected = isSuccessfullyDisconnected
                });
            }
        }

        public async Task<HiveModel> GetHiveModel(string hiveId)
        {
            var result = await _redisService.GetAsync<HiveModel>(GetHiveKey(hiveId));
            return result;
        }

        public async Task<InterferenceModel> GetInterferenceModel(Guid interferenceId)
        {
            var result = await _redisService.GetAsync<InterferenceModel>(GetInterferenceKey(interferenceId));
            return result;
        }

        public async Task<List<HiveModel>> GetAllHives()
        {
            var result = await _redisService.GetAllAsync<HiveModel>($"{_redisKeys.HiveKey}:");
            return result;
        }

        public async Task<List<InterferenceModel>> GetAllInterferences()
        {
            var result = await _redisService.GetAllAsync<InterferenceModel>($"{_redisKeys.InterferenceKey}:");
            return result;
        }

        public async Task<HiveOperationalArea> ConnectHive(HiveModel model)
        {
            bool isHiveAlreadyConnected = await IsHiveConnected(model.HiveID);
            if (isHiveAlreadyConnected)
            {
                _logger.LogWarning("Reconnect Hive request: {@model}", model);
            }
            else
            {
                _logger.LogInformation("Trying to connect Hive: {@model}", model);
            }
            bool result = await _redisService.SetAsync(GetHiveKey(model.HiveID), model);
            if (result)
            {
                _logger.LogInformation("Successfully connected Hive: {@model}", model);
                var operationalArea = _spatialService.GetHiveOperationalArea(model);
                if (isHiveAlreadyConnected)
                {
                    await _messageBus.Publish(new HiveReconnectedMessage
                    {
                        HiveID = model.HiveID,
                        Hive = model,
                        InitialOperationalArea = operationalArea,
                        IsSuccessfullyReconnected = result
                    });
                }
                else
                {
                    await _messageBus.Publish(new HiveConnectedMessage
                    {
                        HiveID = model.HiveID,
                        Hive = model,
                        InitialOperationalArea = operationalArea,
                        IsSuccessfullyConnected = result
                    });
                }
                return operationalArea;
            }
            else
            {
                _logger.LogError("Failed to connect Hive: {@model}", model);
                throw new HiveConnectionException($"Failed to connect hive for HiveId: {model.HiveID}");
            }
        }

        public async Task<bool> IsHiveConnected(string hiveId)
        {
            string hiveKey = GetHiveKey(hiveId);
            return await _redisService.CheckIfKeyExists(hiveKey);
        }

        public async Task<DateTime> AddTelemetry(HiveTelemetryModel model)
        {
            string hiveKey = GetHiveKey(model.HiveID);
            bool result = await _redisService.UpdateAsync(hiveKey, (HiveModel hive) =>
            {
                hive.Telemetry = model;
            });

            if (result)
            {
                _logger.LogInformation("Telemetry updated for HiveID: {hiveId}. Updated telemetry timestamp: {timestamp}", model.HiveID, model.Timestamp);
            }
            else
            {
                _logger.LogError("Failed to update Telemetry - Redis update issue. HiveID: {hiveId}, Telemetry model: {@telemetry}", model.HiveID, model);
            }

            await _messageBus.Publish(new TelemetrySentMessage
            {
                HiveID = model.HiveID,
                Telemetry = model,
                IsSuccessfullySent = result
            });
            return model.Timestamp;
        }

        public async Task<string> SendHiveStopSignal(string hiveId)
        {
            var hive = await GetHiveModel(hiveId);
            if (hive == null)
            {
                _logger.LogError("Sending Hive Stop signal: Hive not found for HiveID: {hiveId}", hiveId);
                return null;
            }

            bool isSuccessfullySent = false;
            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            var command = new StopHiveMindCommand
            {
                CommandType = HiveMindState.Stop,
                StopImmediately = true,
                Timestamp = DateTime.Now
            };
            try
            {
                var result = await _hiveHttpClient.SendHiveControlCommandAsync(hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);
                isSuccessfullySent = true;
                return result;
            }
            finally
            {
                if (isSuccessfullySent)
                {
                    await _messageBus.Publish(new StopHiveMessage
                    {
                        IsImmediateStop = true,
                        HiveID = hiveId
                    });
                }
                else
                {
                    _logger.LogError("Failed to send stop command for Hive: {@hive}, path: {path}, \n Command: {@command}", hive, hiveMindPath, command);
                }

            }
        }

        public async Task<string> SendHiveControlSignal(string hiveId, Location destination)
        {
            var hive = await GetHiveModel(hiveId);
            if (hive == null)
            {
                _logger.LogError("Sending Hive Control signal: Hive not found for HiveID: {hiveId}", hiveId);
                return null;
            }

            bool isSuccessfullySent = false;
            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            var command = new MoveHiveMindCommand
            {
                CommandType = HiveMindState.Move,
                Destination = destination,
                Timestamp = DateTime.Now
            };
            try
            {
                var result = await _hiveHttpClient.SendHiveControlCommandAsync(hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);
                isSuccessfullySent = true;
                return result;
            }
            finally
            {
                if (isSuccessfullySent)
                {
                    await _messageBus.Publish(new MoveHiveMessage
                    {
                        IsSuccessfullySent = isSuccessfullySent,
                        Destination = destination,
                        HiveID = hiveId
                    });
                }
                else
                {
                    _logger.LogError("Failed to send control command for Hive: {@hive}, path: {path}, \n Command: {@command}", hive, hiveMindPath, command);
                }
                
            }
        }

        private string GetHiveKey(string hiveId)
        {
            return $"{_redisKeys.HiveKey}:{hiveId}";
        }

        #region Interference
        public async Task<Guid> SetInterference(InterferenceModel model)
        {
            bool result = await _redisService.SetAsync(GetInterferenceKey(model.Id), model);
            if (result)
            {
                _logger.LogInformation("Successfully added interference: {@model}", model);
            }
            else
            {
                _logger.LogError("Failed to connect add Interference: {@model}", model);
                throw new HiveConnectionException("Failed to add interference");
            }

            return model.Id;
        }

        public async Task<bool> DeleteInterference(Guid interferenceId)
        {
            var result = await _redisService.DeleteAsync(GetInterferenceKey(interferenceId));
            return result;
        }

        public async Task NotifyHivesOnDeletedInterference(Guid interferenceId)
        {
            var hives = await GetAllHives();
            var interference = await GetInterferenceModel(interferenceId);

            if (interference is not null)
            {
                _logger.LogError("Interference {interferenceId} was not deleted, notification is not feasible", interferenceId);
                return;
            }

            if (hives.Count == 0)
            {
                _logger.LogInformation("No hives to notify about deleted interference {interferenceId}", interferenceId);
                return;
            }

            var command = new DeleteInterferenceFromHiveMindCommand
            {
                CommandType = HiveMindState.DeleteInterference,
                InterferenceId = interferenceId
            };

            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            string[] hiveIds = hives.Select(h => h.HiveID).ToArray();
            _logger.LogInformation("Notifying {count} hives about deleted interference {interferenceId}: {hiveIds}", hives.Count, interferenceId, hiveIds);

            var notificationTasks = hives.Select(async hive =>
            {
                try
                {
                    var result = await _hiveHttpClient.SendHiveControlCommandAsync(
                        hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);

                    _logger.LogInformation(
                        "Successfully notified hive {hiveId} about deleted interference {interferenceId}",
                        hive.HiveID, interferenceId);

                    return (HiveId: hive.HiveID, Success: true, Error: (Exception)null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to notify hive {hiveId} about deleted interference {interferenceId}",
                        hive.HiveID, interferenceId);

                    return (HiveId: hive.HiveID, Success: false, Error: ex);
                }
            });

            var results = await Task.WhenAll(notificationTasks);

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            if (failureCount > 0)
            {
                var failedHives = string.Join(", ", results.Where(r => !r.Success).Select(r => r.HiveId));
                _logger.LogWarning(
                    "Deleted nterference notification complete for {interferenceId}: {success}/{total} succeeded. Failed hives: {failedHives}",
                    interferenceId, successCount, hives.Count, failedHives);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully notified all {total} hives about deleted interference {interferenceId}",
                    hives.Count, interferenceId);
            }
        }
        public async Task NotifyHivesAboutAddedInterference(Guid interferenceId)
        {
            var hives = await GetAllHives();
            var interference = await GetInterferenceModel(interferenceId);

            if (interference == null)
            {
                _logger.LogError("Interference {interferenceId} not found for notification", interferenceId);
                return;
            }

            if (hives.Count == 0)
            {
                _logger.LogInformation("No hives to notify about interference {interferenceId}", interferenceId);
                return;
            }

            var command = new AddInterferenceToHiveMindCommand
            {
                CommandType = HiveMindState.SetInterference,
                Interference = interference,
            };

            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            string[] hiveIds = hives.Select(h => h.HiveID).ToArray();
            _logger.LogInformation("Notifying {count} hives about interference {interferenceId}: {hiveIds}", hives.Count, interferenceId, hiveIds);

            var notificationTasks = hives.Select(async hive =>
            {
                try
                {


                    var result = await _hiveHttpClient.SendHiveControlCommandAsync(
                        hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);
                    
                    _logger.LogInformation(
                        "Successfully notified hive {hiveId} about interference {interferenceId}", 
                        hive.HiveID, interferenceId);
                    
                    return (HiveId: hive.HiveID, Success: true, Error: (Exception)null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to notify hive {hiveId} about interference {interferenceId}", 
                        hive.HiveID, interferenceId);
                    
                    return (HiveId: hive.HiveID, Success: false, Error: ex);
                }
            });

            var results = await Task.WhenAll(notificationTasks);

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);
            
            if (failureCount > 0)
            {
                var failedHives = string.Join(", ", results.Where(r => !r.Success).Select(r => r.HiveId));
                _logger.LogWarning(
                    "Interference notification complete for {interferenceId}: {success}/{total} succeeded. Failed hives: {failedHives}",
                    interferenceId, successCount, hives.Count, failedHives);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully notified all {total} hives about interference {interferenceId}",
                    hives.Count, interferenceId);
            }
        }

        private string GetInterferenceKey(Guid interferenceId)
        {
            return $"{_redisKeys.InterferenceKey}:{interferenceId.ToString()}";
        }

        #endregion
    }
}