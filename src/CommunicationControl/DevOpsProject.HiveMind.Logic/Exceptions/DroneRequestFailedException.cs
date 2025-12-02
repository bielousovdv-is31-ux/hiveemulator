namespace DevOpsProject.HiveMind.Logic.Exceptions;

public sealed class DroneRequestFailedException : Exception
{
    public DroneRequestFailedException(string message) : base(message)
    {
        
    }
    
    public DroneRequestFailedException(string message, Exception inner) : base(message, inner)
    {
    }
}
