import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import { getDatabase, ref, get } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-database.js";
import { firebaseConfig } from "./firebase-config.js";

const app = initializeApp(firebaseConfig);
const database = getDatabase(app);

const loading = document.getElementById("loading");
const statusText = document.getElementById("statusText");
const topicBadge = document.getElementById("topicBadge");
const marker = document.getElementById("marker");
const bubbleTitle = document.getElementById("bubbleTitle");
const bubblePoints = document.getElementById("bubblePoints");

let currentPoints = [];
let currentIndex = 0;

const params = new URLSearchParams(window.location.search);
const topicId = params.get("topic");

main();

async function main() {
  if (!topicId) {
    showBlockingMessage("No topic was provided. Please scan a valid AR cheatsheet QR code.");
    return;
  }

  try {
    statusText.textContent = "Loading cheatsheet...";

    const data = await loadCheatsheet(topicId);

    if (!data) {
      showBlockingMessage(`No cheatsheet found for topic: ${topicId}`);
      return;
    }

    renderCheatsheet(data);
    setupMarkerEvents(data.title);
    setupInteractivity();

    statusText.textContent = "Point your camera at the AR marker.";
    setTimeout(() => {
      loading.hidden = true;
    }, 1200);
  } catch (error) {
    console.error(error);
    showBlockingMessage("Unable to load this cheatsheet. Check your internet connection and Firebase setup.");
  }
}

async function loadCheatsheet(id) {
  const normalizedId = id.replaceAll("-", "_");

  const possiblePaths = [
    `playbook/cheatsheets/${id}`,
    `playbook/cheatsheets/${normalizedId}`,
    `cheatsheets/${id}`,
    `cheatsheets/${normalizedId}`
  ];

  for (const path of possiblePaths) {
    console.log("Trying Firebase path:", path);
    const snapshot = await get(ref(database, path));

    if (snapshot.exists()) {
      console.log("Loaded cheatsheet from:", path);
      return snapshot.val();
    }
  }

  return null;
}

function renderCheatsheet(data) {
    currentPoints = Array.isArray(data.points) ? data.points : Object.values(data.points || {});
    currentIndex = 0;

    bubbleTitle.setAttribute("value", data.title || "Cheatsheet");
    
    if (currentPoints.length > 0) {
        updateBubbleText();
    } else {
        bubblePoints.setAttribute("value", "No points available.");
    }

    topicBadge.textContent = `${data.title} loaded. Tap to cycle points!`;
    topicBadge.hidden = false;
}

function updateBubbleText() {
    const pointText = `${currentIndex + 1}/${currentPoints.length}: ${currentPoints[currentIndex]}`;
    bubblePoints.setAttribute("value", pointText);
}

function setupInteractivity() {
    const bubbleBack = document.getElementById("bubbleBack");
    const otter = document.getElementById("otter");

    function cyclePoint() {
        if (currentPoints.length <= 1) return; 
        currentIndex = (currentIndex + 1) % currentPoints.length;
        updateBubbleText();
    }

    if (bubbleBack) bubbleBack.addEventListener("click", cyclePoint);
    if (otter) otter.addEventListener("click", cyclePoint);
}

function setupMarkerEvents(title) {
    marker.addEventListener("markerFound", () => {
        topicBadge.textContent = `${title} marker detected. Tap the screen to read!`;
    });

    marker.addEventListener("markerLost", () => {
        topicBadge.textContent = "Marker lost. Point your camera back at the printed AR marker.";
    });
}

function showBlockingMessage(message) {
    statusText.textContent = message;
    loading.hidden = false;
}