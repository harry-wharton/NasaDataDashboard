// wwwroot/js/asteroids3d.mjs

// This module includes Three js for rendering an interactive asteroid field
// and has orbit controls, clickable asteroids to bring up an info panel

// Imports
import * as THREE from 'https://esm.sh/three@0.152.2';
import { OrbitControls } from 'https://esm.sh/three@0.152.2/examples/jsm/controls/OrbitControls.js';

// Threejs stuff
let scene, camera, renderer, controls;

// Scene objects
let asteroidObjects = [];
let sunMesh = null;

// Interactivity
let raycaster, mouse;
let infoPanel = null;

export function renderAsteroids(visualData) {

    // Debug Logs
    console.log("Received asteroids:", visualData);

    if (visualData.length > 0) {
        console.log("First asteroid structure:", visualData[0]);
    }

    const container = document.getElementById('asteroid-3d-canvas');
    if (!container) {
        console.error("Container not found!");
        return;
    }

    // One time Threejs setup
    // Initialize scene if not already
    if (!scene) {
        scene = new THREE.Scene();
        scene.background = new THREE.Color(0x000011);

        const width = container.clientWidth;
        const height = container.clientHeight;

        // Assign perspective camera looking towards origin
        camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
        camera.position.set(5, 5, 10);

        // Enable anti aliasing, this will smooth out edges but
        // might make performance worse, worth remembering
        renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(width, height);
        container.appendChild(renderer.domElement);

        // Global light + central point light acting as the sun
        const ambient = new THREE.AmbientLight(0x404040);
        scene.add(ambient);

        const sunLight = new THREE.PointLight(0xffffff, 1.5);
        sunLight.position.set(0, 0, 0);
        scene.add(sunLight);

        // Orbit camera controlled with mouse
        controls = new OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;
        controls.dampingFactor = 0.05;
        controls.target.set(0, 0, 0);

        // Initialize raycaster and mouse for clicking
        raycaster = new THREE.Raycaster();
        mouse = new THREE.Vector2();

        // Create info panel
        createInfoPanel(container);

        // Add click event listener
        renderer.domElement.addEventListener('click', onAsteroidClick, false);

        animate();
    }

    // Remove previous asteroid meshes and free GPU resources
    asteroidObjects.forEach(obj => {
        obj.geometry.dispose();
        obj.material.dispose();
        scene.remove(obj);
    });
    asteroidObjects = [];


    console.log("Processing", visualData.length, "asteroids");

    // Determine bounds so asteroids can be scaled to fit the scene
    let minX = Infinity, maxX = -Infinity;
    let minY = Infinity, maxY = -Infinity;
    let minZ = Infinity, maxZ = -Infinity;

    visualData.forEach(ast => {
        const pos = ast.position || ast.Position;
        if (!pos) return;

        const x = Number(pos.x || pos.X);
        const y = Number(pos.y || pos.Y);
        const z = Number(pos.z || pos.Z);

        if (isNaN(x) || isNaN(y) || isNaN(z)) return;

        minX = Math.min(minX, x); maxX = Math.max(maxX, x);
        minY = Math.min(minY, y); maxY = Math.max(maxY, y);
        minZ = Math.min(minZ, z); maxZ = Math.max(maxZ, z);
    });

    if (!isFinite(minX) || !isFinite(maxX)) {
        console.error("Could not determine data bounds");
        return;
    }

    const maxDimension = Math.max(
        Math.abs(maxX - minX),
        Math.abs(maxY - minY),
        Math.abs(maxZ - minZ)
    );

    // Scale all positions so the largest dimension fits roughly within 5 units
    const scaleFactor = maxDimension > 0 ? 10 / maxDimension : 1;

    // Find asteroid size range for log scaling
    let minSize = Infinity, maxSize = -Infinity;

    // Map raw sizes to visually meaningful radius using log scaling
    visualData.forEach(ast => {
        const size = Number(ast.size || ast.Size);
        if (!isNaN(size) && size > 0) {
            minSize = Math.min(minSize, size);
            maxSize = Math.max(maxSize, size);
        }
    });

    /*
    Sun 
    */
    const sunRadius = 0.3;

    if (sunMesh) {
        scene.remove(sunMesh);
    }

    const sunGeometry = new THREE.SphereGeometry(sunRadius, 32, 32);
    const sunMaterial = new THREE.MeshBasicMaterial({
        color: 0xffaa00,
        emissive: 0xffaa00,
        emissiveIntensity: 1
    });
    sunMesh = new THREE.Mesh(sunGeometry, sunMaterial);
    scene.add(sunMesh);

    const glowGeometry = new THREE.SphereGeometry(sunRadius * 1.3, 32, 32);
    const glowMaterial = new THREE.MeshBasicMaterial({
        color: 0xff6600,
        transparent: true,
        opacity: 0.3
    });
    const glowMesh = new THREE.Mesh(glowGeometry, glowMaterial);
    sunMesh.add(glowMesh);

    // Asteroid mesh creation
    visualData.forEach((ast, index) => {
        const pos = ast.position || ast.Position;
        if (!pos) return;

        const x = Number(pos.x || pos.X);
        const y = Number(pos.y || pos.Y);
        const z = Number(pos.z || pos.Z);

        if (isNaN(x) || isNaN(y) || isNaN(z)) return;

        const scaledX = x * scaleFactor;
        const scaledY = y * scaleFactor;
        const scaledZ = z * scaleFactor;

        const rawSize = Number(ast.size || ast.Size);

        let visualSize;
        if (maxSize > minSize) {
            const logMin = Math.log(minSize);
            const logMax = Math.log(maxSize);
            const logRaw = Math.log(Math.max(rawSize, minSize));
            const normalizedLog = (logRaw - logMin) / (logMax - logMin);
            visualSize = 0.03 + normalizedLog * 0.12;
        } else {
            visualSize = 0.06;
        }

        // Set vertices randomly to create a kinda asteroidy shape
        const geometry = new THREE.DodecahedronGeometry(visualSize, 0);

        const positionAttribute = geometry.attributes.position;
        const vertex = new THREE.Vector3();
        for (let i = 0; i < positionAttribute.count; i++) {
            vertex.fromBufferAttribute(positionAttribute, i);
            vertex.normalize();
            const randomScale = 0.8 + Math.random() * 0.4;
            vertex.multiplyScalar(visualSize * randomScale);
            positionAttribute.setXYZ(i, vertex.x, vertex.y, vertex.z);
        }
        geometry.computeVertexNormals();

        // Choose random asteroid colour variation
        const colorVariation = Math.random();
        let asteroidColor;
        if (colorVariation < 0.33) {
            asteroidColor = 0x8b7355;
        } else if (colorVariation < 0.66) {
            asteroidColor = 0x696969;
        } else {
            asteroidColor = 0x9c8b7b;
        }

        const material = new THREE.MeshPhongMaterial({
            color: asteroidColor,
            emissive: 0x000000,
            shininess: 5,
            flatShading: true
        });

        const mesh = new THREE.Mesh(geometry, material);
        mesh.position.set(scaledX, scaledY, scaledZ);

        mesh.rotation.x = Math.random() * Math.PI * 2;
        mesh.rotation.y = Math.random() * Math.PI * 2;
        mesh.rotation.z = Math.random() * Math.PI * 2;

        // Store metadata for animation and interaction
        mesh.userData.rotationSpeed = {
            x: (Math.random() - 0.5) * 0.01,
            y: (Math.random() - 0.5) * 0.01,
            z: (Math.random() - 0.5) * 0.01
        };

        // Store data for info display
        mesh.userData.asteroidData = ast;
        mesh.userData.originalColor = asteroidColor;

        scene.add(mesh);
        asteroidObjects.push(mesh);
    });

    console.log("Total asteroids rendered:", asteroidObjects.length);

    controls.target.set(0, 0, 0);
    camera.lookAt(0, 0, 0);
}

// Info panel created on clicking asteroids
function createInfoPanel(container) {
    infoPanel = document.createElement('div');
    infoPanel.id = 'asteroid-info-panel';
    infoPanel.style.cssText = `
        position: absolute;
        top: 20px;
        right: 20px;
        background: rgba(0, 0, 0, 0.85);
        color: white;
        padding: 15px;
        border-radius: 8px;
        font-family: Arial, sans-serif;
        font-size: 14px;
        max-width: 300px;
        display: none;
        border: 2px solid #444;
        box-shadow: 0 4px 8px rgba(0,0,0,0.5);
        z-index: 1000;
    `;
    container.parentElement.style.position = 'relative';
    container.parentElement.appendChild(infoPanel);
}

function onAsteroidClick(event) {
    console.log("Click event fired!");
    console.log("Renderer element:", renderer.domElement);
    console.log("Number of asteroid objects:", asteroidObjects.length);

    const rect = renderer.domElement.getBoundingClientRect();
    mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    console.log("Mouse coordinates:", mouse.x, mouse.y);

    raycaster.setFromCamera(mouse, camera);
    const intersects = raycaster.intersectObjects(asteroidObjects);

    console.log("Intersects found:", intersects.length);

    if (intersects.length > 0) {
        console.log("Asteroid clicked! Calling showAsteroidInfo...");
        const clickedAsteroid = intersects[0].object;
        showAsteroidInfo(clickedAsteroid);

        // Highlight selected asteroid
        asteroidObjects.forEach(obj => {
            obj.material.emissive.setHex(0x000000);
        });
        clickedAsteroid.material.emissive.setHex(0x444444);
    } else {
        console.log("No asteroid clicked, hiding info panel");
        hideAsteroidInfo();
        asteroidObjects.forEach(obj => {
            obj.material.emissive.setHex(0x000000);
        });
    }
}
function showAsteroidInfo(asteroidMesh) {
    const data = asteroidMesh.userData.asteroidData;

    console.log("Full asteroid data:", data);
    console.log("Data keys:", Object.keys(data));
    console.log("Raw diameter value:", data.diameter, "Type:", typeof data.diameter);
    console.log("Raw Diameter value:", data.Diameter, "Type:", typeof data.Diameter);

    const pos = data.position || data.Position;
    const size = data.size || data.Size;

    // Extract additional data fields
    const name = data.name || data.Name;
    const diameter = data.diameter || data.Diameter;
    const distanceFromSun = data.distanceFromSun || data.DistanceFromSun;
    const orbitalPeriod = data.orbitalPeriod || data.OrbitalPeriod;
    const isPHA = data.isPotentiallyHazardous || data.IsPotentiallyHazardous || false;

    console.log("Extracted values:", {
        name,
        diameter,
        distanceFromSun,
        orbitalPeriod,
        isPHA
    });

    // Calculate distance from origin (represents distance from sun in scene)
    const sceneDistance = Math.sqrt(
        Math.pow(asteroidMesh.position.x, 2) +
        Math.pow(asteroidMesh.position.y, 2) +
        Math.pow(asteroidMesh.position.z, 2)
    );

    let html = '<h3 style="margin: 0 0 10px 0; color: #ffa500;">Asteroid Info</h3>';

    // Name - show if available and not empty
    if (name && name.trim() !== '') {
        html += `<p style="margin: 5px 0;"><strong style="color: #ffcc66;">${name}</strong></p>`;
    }

    // Hazard warning
    if (isPHA) {
        html += `<p style="margin: 8px 0; padding: 5px; background: rgba(255, 0, 0, 0.2); border: 1px solid #ff0000; border-radius: 4px; font-size: 12px;">
            Potentially Hazardous
        </p>`;
    }

    // Physical characteristics section
    html += '<div style="margin-top: 10px; padding-top: 10px; border-top: 1px solid #444;">';
    html += '<p style="margin: 5px 0; color: #aaa; font-size: 12px;"><strong>Physical Properties</strong></p>';

    // Check if diameter exists and is a valid number
    if (diameter != null && !isNaN(Number(diameter)) && Number(diameter) > 0) {
        html += `<p style="margin: 5px 0;"><strong>Diameter:</strong> ${Number(diameter).toFixed(2)} km</p>`;
    }

    html += `<p style="margin: 5px 0;"><strong>Relative Size:</strong> ${Number(size).toFixed(4)}</p>`;
    html += '</div>';

    // Orbital information section
    html += '<div style="margin-top: 10px; padding-top: 10px; border-top: 1px solid #444;">';
    html += '<p style="margin: 5px 0; color: #aaa; font-size: 12px;"><strong>Orbital Properties</strong></p>';

    // Check if distanceFromSun exists and is valid
    if (distanceFromSun != null && !isNaN(Number(distanceFromSun)) && Number(distanceFromSun) > 0) {
        const distNum = Number(distanceFromSun);

        // Check if the value is likely in kilometers (very large number) or AU
        if (distNum > 1000000) {
            // Likely in kilometers, convert to AU
            const distanceInAU = distNum / 149597871;
            html += `<p style="margin: 5px 0;"><strong>Distance from Sun:</strong> ${distanceInAU.toFixed(3)} AU</p>`;
            html += `<p style="margin: 5px 0; font-size: 11px; color: #888;">(${(distNum / 1000000).toFixed(2)} million km)</p>`;
        } else {
            // Already in AU
            html += `<p style="margin: 5px 0;"><strong>Distance from Sun:</strong> ${distNum.toFixed(3)} AU</p>`;
        }
    } else {
        html += `<p style="margin: 5px 0;"><strong>Scene Distance:</strong> ${sceneDistance.toFixed(2)} units</p>`;
    }

    // Check if orbitalPeriod exists and is valid
    if (orbitalPeriod != null && !isNaN(Number(orbitalPeriod)) && Number(orbitalPeriod) > 0) {
        const periodNum = Number(orbitalPeriod);

        // Check if period is in days or years
        if (periodNum > 100) {
            // Likely in days, convert to years
            const periodInYears = periodNum / 365.25;
            html += `<p style="margin: 5px 0;"><strong>Orbital Period:</strong> ${periodInYears.toFixed(2)} years</p>`;
            html += `<p style="margin: 5px 0; font-size: 11px; color: #888;">(${periodNum.toFixed(0)} days)</p>`;
        } else {
            // Already in years
            html += `<p style="margin: 5px 0;"><strong>Orbital Period:</strong> ${periodNum.toFixed(2)} years</p>`;
        }
    }

    html += '</div>';

    // Position data collapsed
    html += '<div style="margin-top: 10px; padding-top: 10px; border-top: 1px solid #444;">';
    html += '<details style="cursor: pointer;">';
    html += '<summary style="color: #888; font-size: 11px;">Technical Position Data</summary>';
    html += `<p style="margin: 5px 0; font-size: 11px; color: #aaa;">
        X: ${Number(pos.x || pos.X).toFixed(4)}<br/>
        Y: ${Number(pos.y || pos.Y).toFixed(4)}<br/>
        Z: ${Number(pos.z || pos.Z).toFixed(4)}
    </p>`;
    html += '</details>';
    html += '</div>';

    html += `<p style="font-size: 11px; color: #666; margin-top: 12px; text-align: center;">Click elsewhere to deselect</p>`;

    infoPanel.innerHTML = html;
    infoPanel.style.display = 'block';
}

function hideAsteroidInfo() {
    infoPanel.style.display = 'none';
}

function animate() {
    requestAnimationFrame(animate);

    // Rotate all asteroids
    asteroidObjects.forEach(obj => {
        if (obj.userData.rotationSpeed) {
            obj.rotation.x += obj.userData.rotationSpeed.x;
            obj.rotation.y += obj.userData.rotationSpeed.y;
            obj.rotation.z += obj.userData.rotationSpeed.z;
        }
    });

    if (controls) controls.update();
    if (renderer && scene && camera) renderer.render(scene, camera);
}