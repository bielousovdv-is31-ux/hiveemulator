import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Fetch the center coordinates for the initial map load
export const fetchCenterCoordinates = async (apiUrl) => {
    try {
        const response = await axios.get(`${apiUrl}/area`);
        return response.data;
    } catch (error) {
        console.error("Error fetching center coordinates:", error);
        return null;
    }
};

// Fetch all hives and extract their latitude/longitude
export const fetchHives = async (apiUrl) => {
    try {
        const response = await axios.get(`${apiUrl}/hive`);

        return response.data.map(hive => ({
            id: hive.HiveID,
            lat: hive.Telemetry?.Location?.Latitude ?? null,
            lon: hive.Telemetry?.Location?.Longitude ?? null,
        })).filter(hive => hive.lat !== null && hive.lon !== null); // Remove invalid locations

    } catch (error) {
        console.error("Error fetching hives:", error);
        return [];
    }
};

// Move all hives to a new location
export const moveHives = async (apiUrl, lat, lon, ids) => {
    try {
        await axios.patch(`${apiUrl}/hive`, { 
            Hives: ids, 
            Destination: {
                Latitude: lat,
                Longitude: lon
            } 
        });
        console.log(`Moved hives to: ${lat}, ${lon}`);
    } catch (error) {
        console.error("Error moving hives:", error);
        throw error;
    }
};

export const fetchInterferences = async (apiUrl) => {
    try {
        const response = await axios.get(`${apiUrl}/interferences`);
        
        // Map backend response to UI format
        // Backend: { Id, RadiusKM, Location: { Latitude, Longitude }, CreatedAt }
        // UI needs: { id, lat, lon, radiusMeters }
        const interferences = response.data.map(interference => ({
            id: interference.Id,
            lat: interference.Location?.Latitude ?? null,
            lon: interference.Location?.Longitude ?? null,
            radiusKM: interference.RadiusKM,
            radiusMeters: interference.RadiusKM * 1000, // Convert KM to meters for visualization
            createdAt: interference.CreatedAt
        }));

        // Validate and filter out invalid interferences
        const invalid = interferences.filter(i => 
            i.lat === null || i.lon === null || 
            isNaN(i.lat) || isNaN(i.lon) ||
            i.radiusKM <= 0
        );
        
        if (invalid.length > 0) {
            console.warn(`${invalid.length} interferences have invalid data and will be ignored:`, invalid);
        }

        const valid = interferences.filter(i => 
            i.lat !== null && i.lon !== null &&
            !isNaN(i.lat) && !isNaN(i.lon) &&
            i.lat >= -90 && i.lat <= 90 &&
            i.lon >= -180 && i.lon <= 180 &&
            i.radiusKM > 0
        );

        console.log(`Loaded ${valid.length} valid interferences`);
        return valid;
        
    } catch (error) {
        console.error("Error fetching interferences:", error);
        return [];
    }
};

// Add a new interference zone
export const addInterference = async (apiUrl, lat, lon, radiusMeters) => {
    try {
        // Validate inputs before sending to backend
        const latitude = parseFloat(lat);
        const longitude = parseFloat(lon);
        const radius = parseInt(radiusMeters);

        if (isNaN(latitude) || isNaN(longitude) || isNaN(radius)) {
            throw new Error("Invalid input: lat, lon, and radius must be valid numbers");
        }

        if (radius <= 0) {
            throw new Error("Radius must be greater than 0");
        }

        // Convert meters to kilometers for backend
        const radiusKM = radius / 1000;
        
        const response = await axios.post(`${apiUrl}/interference`, {
            RadiusKM: radiusKM,
            Location: {
                Latitude: latitude,
                Longitude: longitude
            }
        });
        
        console.log(`Interference added at: ${lat}, ${lon} with radius ${radiusKM}km (${radius}m)`);
        return response.data;
    } catch (error) {
        console.error("Error adding interference:", error);
        throw error;
    }
};

// Delete an interference zone
export const deleteInterference = async (apiUrl, interferenceId) => {
    try {
        await axios.delete(`${apiUrl}/interference/${interferenceId}`);
        console.log(`Interference ${interferenceId} deleted`);
    } catch (error) {
        console.error("Error deleting interference:", error);
        throw error;
    }
};

export const stopHiveMove = async (apiUrl, ids) => {
    try {
        await axios.post(`${apiUrl}/hive/stop`, {
            Hives: ids
        });
        console.log(`Stop hive move signal sent`);
    }
    catch (error) {
        console.error("Error stopping hive movement:", error);
        throw error;
    }
};