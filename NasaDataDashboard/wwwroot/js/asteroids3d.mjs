// wwwroot/js/asteroids3d.mjs
import * as THREE from 'https://esm.sh/three@0.152.2';
import { OrbitControls } from 'https://esm.sh/three@0.152.2/examples/jsm/controls/OrbitControls.js';

let scene, camera, renderer, controls;
let asteroidObjects = [];

export function renderAsteroids(visualData) {
    console.log("Received asteroids:", visualData);

    // Debug: Check first asteroid structure
    if (visualData.length > 0) {
        console.log("First asteroid structure:", visualData[0]);
        console.log("First asteroid keys:", Object.keys(visualData[0]));
        if (visualData[0].position || visualData[0].Position) {
            const pos = visualData[0].position || visualData[0].Position;
            console.log("Position object:", pos);
            console.log("Position keys:", Object.keys(pos));
        }
    }

    const container = document.getElementById('asteroid-3d-canvas');
    if (!container) {
        console.error("Container not found!");
        return;
    }

    // Initialize scene if not already
    if (!scene) {
        scene = new THREE.Scene();
        scene.background = new THREE.Color(0x000011);

        const width = container.clientWidth;
        const height = container.clientHeight;

        camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
        camera.position.set(5, 5, 10);

        renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(width, height);
        container.appendChild(renderer.domElement);

        const ambient = new THREE.AmbientLight(0x555555);
        scene.add(ambient);

        const sunLight = new THREE.PointLight(0xffffff, 1.2);
        sunLight.position.set(0, 0, 0);
        scene.add(sunLight);

        // Add a Sun at origin
        const sunGeometry = new THREE.SphereGeometry(0.5, 32, 32);
        const sunMaterial = new THREE.MeshBasicMaterial({ color: 0xffff00 });
        const sun = new THREE.Mesh(sunGeometry, sunMaterial);
        scene.add(sun);

        // Add grid for reference
        const gridHelper = new THREE.GridHelper(20, 20);
        scene.add(gridHelper);

        controls = new OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;
        controls.dampingFactor = 0.05;

        animate();
    }

    // Remove previous asteroids
    asteroidObjects.forEach(obj => scene.remove(obj));
    asteroidObjects = [];

    console.log("Processing", visualData.length, "asteroids");

    // Calculate bounds
    let minX = Infinity, maxX = -Infinity;
    let minY = Infinity, maxY = -Infinity;
    let minZ = Infinity, maxZ = -Infinity;

    visualData.forEach(ast => {
        
        const pos = ast.position || ast.Position;
        if (!pos) {
            console.warn("No position data for asteroid:", ast);
            return;
        }

        const x = Number(pos.x || pos.X);
        const y = Number(pos.y || pos.Y);
        const z = Number(pos.z || pos.Z);

        if (isNaN(x) || isNaN(y) || isNaN(z)) {
            console.warn("Invalid position values:", { x, y, z }, pos);
            return;
        }

        minX = Math.min(minX, x); maxX = Math.max(maxX, x);
        minY = Math.min(minY, y); maxY = Math.max(maxY, y);
        minZ = Math.min(minZ, z); maxZ = Math.max(maxZ, z);
    });

    console.log("Data bounds:", {
        x: [minX, maxX],
        y: [minY, maxY],
        z: [minZ, maxZ]
    });

    // Check if we got valid bounds
    if (!isFinite(minX) || !isFinite(maxX)) {
        console.error("Could not determine data bounds - no valid position data found");
        return;
    }

    // Determine scale factor
    const maxDimension = Math.max(
        Math.abs(maxX - minX),
        Math.abs(maxY - minY),
        Math.abs(maxZ - minZ)
    );

    const scaleFactor = maxDimension > 0 ? 10 / maxDimension : 1;
    console.log("Scale factor:", scaleFactor, "Max dimension:", maxDimension);

    // Add new asteroids
    let successCount = 0;
    visualData.forEach((ast, index) => {
        const pos = ast.position || ast.Position;
        if (!pos) return;

        const x = Number(pos.x || pos.X);
        const y = Number(pos.y || pos.Y);
        const z = Number(pos.z || pos.Z);

        if (isNaN(x) || isNaN(y) || isNaN(z)) return;

        // Scale the positions
        const scaledX = x * scaleFactor;
        const scaledY = y * scaleFactor;
        const scaledZ = z * scaleFactor;

        // Get size
        const size = Math.max(0.05, Number(ast.size || ast.Size) * 10);

        const geometry = new THREE.SphereGeometry(size, 16, 16);
        const material = new THREE.MeshPhongMaterial({
            color: ast.color || ast.Color || 0xffffff,
            emissive: 0x111111
        });
        const mesh = new THREE.Mesh(geometry, material);
        mesh.position.set(scaledX, scaledY, scaledZ);
        scene.add(mesh);
        asteroidObjects.push(mesh);

        if (index < 3) {
            console.log(`Asteroid ${index}: original=(${x}, ${y}, ${z}), scaled=(${scaledX}, ${scaledY}, ${scaledZ}), size=${size}`);
        }
        successCount++;
    });

    console.log("Total asteroids rendered:", successCount);

    // Position camera to look at center of data
    const centerX = (minX + maxX) / 2 * scaleFactor;
    const centerY = (minY + maxY) / 2 * scaleFactor;
    const centerZ = (minZ + maxZ) / 2 * scaleFactor;

    controls.target.set(centerX, centerY, centerZ);
    camera.lookAt(centerX, centerY, centerZ);
}

function animate() {
    requestAnimationFrame(animate);
    if (controls) controls.update();
    if (renderer && scene && camera) renderer.render(scene, camera);
}