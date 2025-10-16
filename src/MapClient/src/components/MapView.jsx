import React, { useEffect, useRef, useState } from "react";
import "ol/ol.css";
import { Map, View } from "ol";
import TileLayer from "ol/layer/Tile";
import { OSM } from "ol/source";
import VectorLayer from "ol/layer/Vector";
import VectorSource from "ol/source/Vector";
import { fromLonLat, toLonLat } from "ol/proj";
import { Point, Circle } from "ol/geom";
import { Feature } from "ol";
import { Style, Icon, Text, Fill, Stroke } from "ol/style";
import Popup from "./Popup";
import { fetchCenterCoordinates, fetchHives, moveHives, fetchInterferences, addInterference, deleteInterference, stopHiveMove } from "../api/mapService";

// TEST STAGING

// TODO: Hardcoded marker icon path
const MARKER_ICON_URL = "/256x256.png";

const MapView = () => {
    const mapRef = useRef(null);
    const vectorLayerRef = useRef(null);
    const initialized = useRef(false);
    const apiUrl = useRef(null)
    const [hives, setHives] = useState([]);
    const [interferences, setInterferences] = useState([]);
    const [popup, setPopup] = useState({ visible: false, coords: null, type: 'map' });
    const [interferenceRadiusModal, setInterferenceRadiusModal] = useState({ visible: false, coords: null });
    const [mouseCoords, setMouseCoords] = useState({ lat: "", lon: "" });
    const popoverRef = useRef(null);

    useEffect(() => {
        const initializeMap = async () => {

            const res = await fetch('/config.json')
            const data = await res.json()

            apiUrl.current = data.API

            if (initialized.current) return;
            initialized.current = true;

            try {
                const center = await fetchCenterCoordinates(apiUrl.current);
                if (center) {
                    initMap(center.Latitude, center.Longitude);
                    await fetchAndDrawVisualInformation();
                }

                // 🔄 Auto-fetch visual information every 5 seconds
                const interval = setInterval(fetchAndDrawVisualInformation, 5000);
                return () => clearInterval(interval);
            } catch (error) {
                console.error("Error initializing map:", error);
            }
        };

        initializeMap();
    }, []);

    // Initialize OpenLayers Map
    const initMap = (lat, lon) => {
        const map = new Map({
            target: "map-container",
            layers: [new TileLayer({ source: new OSM() })],
            view: new View({ center: fromLonLat([lon, lat]), zoom: 12 }),
        });

        map.on("pointermove", (event) => handleMouseMove(event, map));
        map.on("singleclick", (event) => handleMapClick(event, map));

        mapRef.current = map;
    };

    // Fetch hives and draw them on the map
    const fetchAndDrawHives = async () => {
        try {
            const data = await fetchHives(apiUrl.current);
            setHives(data);
            drawHives(data);
        } catch (error) {
            console.error("Error fetching hives:", error);
        }
    };

    // Fetch all visual information (hives and interferences)
    const fetchAndDrawVisualInformation = async () => {
        await fetchAndDrawHives();
        await fetchAndDrawInterferences();
    };

    // Draw markers for all hives
    const drawHives = (hives) => {
        if (!mapRef.current) return;
        if (vectorLayerRef.current) mapRef.current.removeLayer(vectorLayerRef.current);

        const vectorSource = new VectorSource();
        hives.forEach((hive) => {
            const feature = new Feature({
                geometry: new Point(fromLonLat([hive.lon, hive.lat])),
            });

            feature.setId(hive.id);
            feature.setStyle(
                new Style({
                    image: new Icon({ src: MARKER_ICON_URL, scale: 0.05 }),
                    text: new Text({
                        text: hive.id,
                        fill: new Fill({ color: "#000" }),
                        stroke: new Stroke({ color: "#fff", width: 2 }),
                        offsetY: -20,
                    }),
                })
            );

            feature.set("id", hive.id);
            feature.set("lat", hive.lat);
            feature.set("lon", hive.lon);

            vectorSource.addFeature(feature);
        });

        const vectorLayer = new VectorLayer({ source: vectorSource });
        vectorLayerRef.current = vectorLayer;
        mapRef.current.addLayer(vectorLayer);

        mapRef.current.on("pointermove", (event) => handleMarkerHover(event, mapRef.current));
    };

    // Fetch interferences from backend
    const fetchAndDrawInterferences = async () => {
        try {
            const data = await fetchInterferences(apiUrl.current);
            setInterferences(data);
            drawInterferences(data);
        } catch (error) {
            console.error("Error fetching interferences:", error);
        }
    };

    // Draw interference zones on the map
    const drawInterferences = (interferences) => {
        if (!mapRef.current) return;
        
        // Create interference layer if it doesn't exist
        if (!mapRef.current.interferenceLayer) {
            const interferenceSource = new VectorSource();
            const interferenceLayer = new VectorLayer({ 
                source: interferenceSource,
                // Use style function to handle different feature types
                style: function(feature) {
                    const featureType = feature.get("type");
                    
                    if (featureType === "interference") {
                        // Center point style - red icon
                        return new Style({
                            image: new Icon({
                                src: 'data:image/svg+xml;base64,' + btoa(`
                                    <svg width="20" height="20" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
                                        <circle cx="10" cy="10" r="8" fill="red" stroke="darkred" stroke-width="2"/>
                                    </svg>
                                `),
                                scale: 1
                            })
                        });
                    } else if (featureType === "interference_area") {
                        // Circle area style - transparent light red
                        return new Style({
                            stroke: new Stroke({
                                color: '#ff0000', // Bright red border
                                width: 2
                            }),
                            fill: new Fill({
                                color: 'rgba(255, 100, 100, 0.3)' // Transparent light red - you can see map through it
                            })
                        });
                    }
                    return null;
                }
            });
            mapRef.current.interferenceLayer = interferenceLayer;
            mapRef.current.addLayer(interferenceLayer);
        }

        // Clear existing interference features
        mapRef.current.interferenceLayer.getSource().clear();

        // Add new interference features
        interferences.forEach((interference) => {
            const feature = new Feature({
                geometry: new Point(fromLonLat([interference.lon, interference.lat])),
            });

            feature.setId(`interference_${interference.id}`);
            feature.set("id", interference.id);
            feature.set("lat", interference.lat);
            feature.set("lon", interference.lon);
            feature.set("radiusMeters", interference.radiusMeters);
            feature.set("radiusKM", interference.radiusKM);
            feature.set("type", "interference");

            // Create circle for radius visualization
            const center = fromLonLat([interference.lon, interference.lat]);
            const circleGeometry = new Circle(center, interference.radiusMeters);
            
            const circleFeature = new Feature({
                geometry: circleGeometry,
            });
            
            // Set metadata for the circle feature
            circleFeature.setId(`interference_circle_${interference.id}`);
            circleFeature.set("id", interference.id);
            circleFeature.set("lat", interference.lat);
            circleFeature.set("lon", interference.lon);
            circleFeature.set("radiusMeters", interference.radiusMeters);
            circleFeature.set("radiusKM", interference.radiusKM);
            circleFeature.set("type", "interference_area"); // Different type for click handling

            // Add circle first, then center point (so point appears on top)
            mapRef.current.interferenceLayer.getSource().addFeature(circleFeature);
            mapRef.current.interferenceLayer.getSource().addFeature(feature);
        });
        
        // Add hover event listener for interference layer
        mapRef.current.on("pointermove", (event) => handleMarkerHover(event, mapRef.current));
    };

    // Handle Mouse Move (Show live coordinates)
    const handleMouseMove = (event, map) => {
        if (!map) return;
        const coords = toLonLat(event.coordinate);
        setMouseCoords({
            lat: coords[1].toFixed(6),
            lon: coords[0].toFixed(6),
        });
    };

    // Show popover when hovering over a marker
    const handleMarkerHover = (event, map) => {
        if (!popoverRef.current) return;
        const features = map.getFeaturesAtPixel(event.pixel);
        if (features.length > 0) {
            const feature = features[0];
            const featureType = feature.get("type");
            const featureId = feature.get("id");
            const lat = feature.get("lat");
            const lon = feature.get("lon");
            
            if (featureType === "interference" || featureType === "interference_area") {
                const radiusMeters = feature.get("radiusMeters");
                const radiusKM = feature.get("radiusKM");
                popoverRef.current.innerHTML = `Interference ID: ${featureId}<br>Lat: ${lat}<br>Lon: ${lon}<br>Radius: ${radiusKM}km (${radiusMeters}m)`;
            } else {
                popoverRef.current.innerHTML = `ID: ${featureId}<br>Lat: ${lat}<br>Lon: ${lon}`;
            }
            
            popoverRef.current.style.left = `${event.pixel[0] + 10}px`;
            popoverRef.current.style.top = `${event.pixel[1] + 10}px`;
            popoverRef.current.style.display = "block";
        } else {
            popoverRef.current.style.display = "none";
        }
    };

    // Show popup when clicking an empty spot (Move Hives)
    const handleMapClick = (event, map) => {
        const features = map.getFeaturesAtPixel(event.pixel);
        
        if (features.length > 0) {
            const feature = features[0];
            const featureType = feature.get("type");
            
            if (featureType === "interference") {
                // Handle interference CENTER POINT click - show interference details
                const coords = toLonLat(event.coordinate);
                setPopup({ 
                    visible: true, 
                    coords: { lat: coords[1].toFixed(6), lon: coords[0].toFixed(6) }, 
                    type: 'interference',
                    interferenceId: feature.get("id")
                });
            } else if (featureType === "interference_area") {
                // Handle interference AREA click - treat like map click (place interference/move hives)
                const coords = toLonLat(event.coordinate);
                setPopup({ visible: true, coords: { lat: coords[1].toFixed(6), lon: coords[0].toFixed(6) }, type: 'map' });
            } else {
                // Handle hive click (existing behavior)
                const coords = toLonLat(event.coordinate);
                setPopup({ visible: true, coords: { lat: coords[1].toFixed(6), lon: coords[0].toFixed(6) }, type: 'map' });
            }
        } else {
            // Empty spot click
            const coords = toLonLat(event.coordinate);
            setPopup({ visible: true, coords: { lat: coords[1].toFixed(6), lon: coords[0].toFixed(6) }, type: 'map' });
        }
    };

    // Handle placing interference
    const handlePlaceInterference = (coords) => {
        setInterferenceRadiusModal({ visible: true, coords });
        setPopup({ visible: false });
    };

    // Handle interference radius input
    const handleInterferenceRadiusSubmit = async (radiusMeters) => {
        try {
            const lat = parseFloat(interferenceRadiusModal.coords.lat);
            const lon = parseFloat(interferenceRadiusModal.coords.lon);
            
            // Validate coordinates
            if (isNaN(lat) || isNaN(lon)) {
                alert('Invalid coordinates');
                return;
            }
            
            setInterferenceRadiusModal({ visible: false });
            
            await addInterference(apiUrl.current, lat, lon, radiusMeters);
            await fetchAndDrawInterferences();
            
            // TODO: Temporarily set toFixed(4)
            const radiusKM = (radiusMeters / 1000).toFixed(4);
            alert(`Interference placed at ${lat}, ${lon} with radius ${radiusKM}km (${radiusMeters}m). Hives are being notified.`);
        } catch (error) {
            console.error("Failed to add interference:", error);
            
            // Show detailed error message
            let errorMessage = "Failed to add interference. ";
            if (error.response?.data) {
                errorMessage += error.response.data;
            } else if (error.message) {
                errorMessage += error.message;
            } else {
                errorMessage += "Please try again.";
            }
            alert(errorMessage);
        }
    };

    const handleRemoveInterference = async (interferenceId) => {
        try {
            setPopup({ visible: false });
            
            await deleteInterference(apiUrl.current, interferenceId);
            await fetchAndDrawInterferences();
            
            alert(`Interference ${interferenceId} has been removed. Hives are being notified.`);
        } catch (error) {
            console.error("Failed to delete interference:", error);
            
            // Show detailed error message
            let errorMessage = "Failed to delete interference. ";
            if (error.response?.data) {
                errorMessage += error.response.data;
            } else if (error.message) {
                errorMessage += error.message;
            } else {
                errorMessage += "Please try again.";
            }
            alert(errorMessage);
        }
    };

    return (
        <div style={{ width: "100%", height: "100vh", display: "flex", flexDirection: "column", alignItems: "center" }}>
            <h1>Hive Map</h1>

            {/* Latitude & Longitude Inputs */}
            <div style={{ marginBottom: "10px", display: "flex", gap: "10px" }}>
                <label>Latitude: <input type="text" value={mouseCoords.lat} disabled /></label>
                <label>Longitude: <input type="text" value={mouseCoords.lon} disabled /></label>
            </div>

            {/* Map Container */}
            <div id="map-container" style={{ width: "80%", height: "80vh", border: "1px solid #ddd", position: "relative" }}></div>

            {/* Tooltip for Marker Hover */}
            <div ref={popoverRef} style={{
                position: "absolute",
                display: "none",
                background: "#fff",
                padding: "5px",
                borderRadius: "5px",
                border: "1px solid #000",
                pointerEvents: "none",
                zIndex: 999
            }}></div>

            {/* Move All Hives Popup (Centered Modal) */}
            <Popup 
                isVisible={popup.visible} 
                coords={popup.coords} 
                type={popup.type}
                interferenceId={popup.interferenceId}
                onConfirm={() => moveHives(apiUrl.current, popup.coords.lat, popup.coords.lon, hives.map(h => h.id))} 
                onPlaceInterference={handlePlaceInterference}
                onRemoveInterference={handleRemoveInterference}
                onCancel={() => setPopup({ visible: false })}
                onStopMove={() => stopHiveMove(apiUrl.current, hives.map(h => h.id))}
            />

            {/* Interference Radius Modal */}
            {interferenceRadiusModal.visible && (
                <div style={{
                    position: "fixed",
                    top: "0",
                    left: "0",
                    width: "100vw",
                    height: "100vh",
                    backgroundColor: "rgba(0, 0, 0, 0.5)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    zIndex: 1000
                }}>
                    <div style={{
                        backgroundColor: "white",
                        padding: "20px",
                        boxShadow: "0px 0px 15px rgba(0,0,0,0.3)",
                        borderRadius: "8px",
                        textAlign: "center",
                        minWidth: "300px"
                    }}>
                        <h3>Set Interference Radius</h3>
                        <p>Lat: {interferenceRadiusModal.coords.lat} | Lon: {interferenceRadiusModal.coords.lon}</p>
                        <input 
                            type="number" 
                            placeholder="Radius in meters" 
                            id="radiusInput"
                            style={{ margin: "10px", padding: "5px", width: "200px" }}
                        />
                        <br />
                        <button 
                            onClick={() => {
                                const radius = document.getElementById('radiusInput').value;
                                const radiusNum = parseInt(radius);
                                
                                // Validate radius
                                if (!radius || isNaN(radiusNum)) {
                                    alert('Please enter a valid number');
                                    return;
                                }
                                
                                if (radiusNum <= 0) {
                                    alert('Radius must be greater than 0 meters');
                                    return;
                                }
                                
                                handleInterferenceRadiusSubmit(radiusNum);
                            }}
                            style={{ marginRight: "10px" }}
                        >
                            Place Interference
                        </button>
                        <button onClick={() => setInterferenceRadiusModal({ visible: false })}>
                            Cancel
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default MapView;
