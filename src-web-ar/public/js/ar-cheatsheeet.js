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

    if (!data || data.active !== true) {
      showBlockingMessage("This cheatsheet is unavailable. Please check that the topic is active in Firebase.");
      return;
    }

    renderCheatsheet(data);
    setupMarkerEvents(data.title);

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
  const snapshot = await get(ref(database, `cheatsheets/${id}`));
  return snapshot.exists() ? snapshot.val() : null;
}

function renderCheatsheet(data) {
  const points = Array.isArray(data.points) ? data.points : Object.values(data.points || {});
  const shortPoints = points.slice(0, 5);

  bubbleTitle.setAttribute("value", data.title || "Cheatsheet");
  bubblePoints.setAttribute("value", shortPoints.map((point) => `- ${point}`).join("\n"));

  topicBadge.textContent = `${data.title}: ${shortPoints.join(" ")}`;
  topicBadge.hidden = false;
}

function setupMarkerEvents(title) {
  marker.addEventListener("markerFound", () => {
    topicBadge.textContent = `${title} marker detected. Keep the marker in view.`;
  });

  marker.addEventListener("markerLost", () => {
    topicBadge.textContent = "Marker lost. Point your camera back at the printed AR marker.";
  });
}

function showBlockingMessage(message) {
  statusText.textContent = message;
  loading.hidden = false;
}