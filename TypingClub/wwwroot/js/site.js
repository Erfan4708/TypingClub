// Existing JavaScript code (SignalR setup and game logic) remains the same.
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/typingHub")
    .build();

let roomId = "";
let textToType = "";
let username = "";
let score = 0;
let playerCars = {};
let timers = {};
let carIndex = 0;
let isCreator = false;
let gameStarted = false;
let completionTimes = {};

// Helper: update status text
function updateStatus(status) {
    const statusElement = document.getElementById("roomStatus");
    statusElement.innerText = "Status: " + status;
    statusElement.style.display = "block";
}

connection.on("RoomCreated", (id, text, userIcons) => {
    roomId = id;
    textToType = text;
    document.getElementById("gameText").innerText = text;
    document.getElementById("gameArea").style.display = "flex";
    document.getElementById("roomIdText").innerText = id;
    document.getElementById("roomIdDisplay").style.display = "flex";
    isCreator = true;
    document.getElementById("startGameButton").style.display = "block";
    updateStatus("Waiting"); // Update status on room creation

    for (const [user, icon] of Object.entries(userIcons)) {
        addCar(user, icon);
    }
});

connection.on("RoomJoined", (text, userIcons) => {
    textToType = text;
    document.getElementById("gameText").innerText = text;
    document.getElementById("gameArea").style.display = "flex";
    updateStatus("Waiting"); // Update status when joining a room

    for (const [user, icon] of Object.entries(userIcons)) {
        addCar(user, icon);
    }
});

connection.on("UserJoined", (user, icon) => {
    addCar(user, icon);
});

connection.on("UpdateScores", (scores) => {
    let leaderboard = document.getElementById("leaderboard");
    leaderboard.innerHTML = "";
    for (const [user, points] of Object.entries(scores)) {
        let li = document.createElement("li");
        li.textContent = `${user}: ${points} points`;
        leaderboard.appendChild(li);
        if (playerCars[user]) {
            let car = playerCars[user];
            car.style.left = `${Math.min((points / textToType.length) * 100, 100)}%`;
            if (points >= textToType.length) {
                car.style.left = "calc(100% - 50px)";
            }
        }
    }
});

connection.on("WinnerAnnounced", (winner, time) => {
    completionTimes[winner] = time;
    stopTimer(winner);
    if (Object.keys(completionTimes).length === Object.keys(playerCars).length) {
        showRankings();
        resetGame();
    }
});

connection.on("StartCountdown", () => {
    let countdown = 7;
    const countdownElement = document.getElementById("countdown");
    const gameTextElement = document.getElementById("gameText");

    // Apply blur effect when countdown starts
    gameTextElement.classList.remove("blurred");
    updateStatus("In Progress"); // Update status when the game starts

    countdownElement.innerText = countdown;
    const interval = setInterval(() => {
        countdown--;
        if (countdown > 0) {
            countdownElement.innerText = countdown;
        } else {
            clearInterval(interval);
            countdownElement.innerText = "";
            startGameForAll();
        }
    }, 1000);
});

connection.on("Error", (message) => {
    alert(message);
});

// Start connection and check URL for room parameter.
connection.start()
    .then(() => {
        const urlParams = new URLSearchParams(window.location.search);
        const roomParam = urlParams.get("room");
        if (roomParam) {
            roomId = roomParam;
            // Assign the extracted roomId to the room input box for visibility.
            const roomInputBox = document.getElementById("roomInput");
            if (roomInputBox) {
                roomInputBox.value = roomId;
            }
            // Get username either from localStorage or prompt the user.
            username = prompt("Enter your username:");
            if (!username) {
                alert("Username is required to join the room.");
                return;
            }
            localStorage.setItem("username", username);
            // Automatically join the room.
            connection.invoke("JoinRoom", roomId, username)
                .then(() => {
                    disableRoomButtons();
                    // Optionally, blur the game text until the game starts.
                    document.getElementById("gameText").classList.add("blurred");
                })
                .catch(err => console.error(err));
        }
    })
    .catch(err => console.error(err));

function createRoom() {
    username = document.getElementById("usernameInput").value.trim();
    if (username === "") {
        alert("Please enter a username.");
        return;
    }
    connection.invoke("CreateRoom", username)
        .then(() => {
            disableRoomButtons(); // Disable controls after successfully creating a room
            const gameTextElement = document.getElementById("gameText");
            gameTextElement.classList.add("blurred");
        })
        .catch(err => console.error(err));
}

function joinRoom() {
    username = document.getElementById("usernameInput").value.trim();
    if (username === "") {
        alert("Please enter a username.");
        return;
    }
    roomId = document.getElementById("roomInput").value;
    connection.invoke("JoinRoom", roomId, username)
        .then(() => {
            disableRoomButtons(); // Disable controls after successfully creating a room
            const gameTextElement = document.getElementById("gameText");
            gameTextElement.classList.add("blurred");
        })
        .catch(err => console.error(err));
}

function startGame() {
    if (!isCreator) return;
    document.getElementById("startGameButton").disabled = true;
    connection.invoke("StartGame", roomId).catch(err => console.error(err));
}

function startGameForAll() {
    gameStarted = true;
    document.getElementById("typingArea").disabled = false;
    for (const user in playerCars) {
        startTimer(user);
    }
}

function checkTyping() {
    if (!gameStarted) return;
    let typedText = document.getElementById("typingArea").value;
    let correctLength = 0;
    for (let i = 0; i < typedText.length; i++) {
        if (typedText[i] === textToType[i]) {
            correctLength++;
        } else {
            break;
        }
    }
    score = correctLength;
    document.getElementById("score").innerText = score;
    const gameText = document.getElementById("gameText");
    gameText.innerHTML = `<strong>${textToType.substring(0, correctLength)}</strong>${textToType.substring(correctLength)}`;
    const typingArea = document.getElementById("typingArea");
    if (typedText.length > correctLength) {
        typingArea.style.borderColor = "red";
    } else {
        typingArea.style.borderColor = "";
    }
    connection.invoke("UpdateProgress", roomId, username, score).catch(err => console.error(err));
}

function addCar(user, iconUrl) {
    if (playerCars[user]) return; // Prevent duplicate cars
    const usernamesDiv = document.getElementById("usernames");
    const raceTrack = document.getElementById("raceTrack");

    // Add username to the usernames section
    const userDiv = document.createElement("div");
    userDiv.classList.add("username-label");
    const nameSpan = document.createElement("span");
    nameSpan.textContent = user;
    const timerSpan = document.createElement("span");
    timerSpan.id = `timer-${user}`;
    timerSpan.classList.add("timer");
    timerSpan.textContent = "00:00";
    userDiv.appendChild(nameSpan);
    userDiv.appendChild(timerSpan);
    usernamesDiv.appendChild(userDiv);

    // Create and position the car
    const car = document.createElement("div");
    car.classList.add("car");
    car.style.top = `${carIndex * 70 + 10}px`; // Position car in its 70px lane
    car.style.left = "0%"; // Starting position
    car.style.backgroundImage = `url('/images/${iconUrl}')`; // Set car image
    raceTrack.appendChild(car);

    // Dynamically adjust race track height
    raceTrack.style.height = `${(carIndex + 1) * 70}px`;

    // Store car reference and increment index
    playerCars[user] = car;
    carIndex++;
}

function startTimer(user) {
    let seconds = 0;
    timers[user] = setInterval(() => {
        seconds++;
        const minutes = Math.floor(seconds / 60);
        const secs = seconds % 60;
        document.getElementById(`timer-${user}`).innerText = `${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }, 1000);
}

function stopTimer(user) {
    clearInterval(timers[user]);
    const timerElement = document.getElementById(`timer-${user}`);
    completionTimes[user] = timerElement.innerText;
}

function showRankings() {
    const sortedTimes = Object.entries(completionTimes).sort((a, b) => {
        const [minA, secA] = a[1].split(":").map(Number);
        const [minB, secB] = b[1].split(":").map(Number);
        return (minA * 60 + secA) - (minB * 60 + secB);
    });
    let rankMessage = "Game Over! Rankings:\n";
    sortedTimes.forEach((entry, index) => {
        const [user, time] = entry;
        rankMessage += `${index + 1}. ${user} - ${time}\n`;
    });
    alert(rankMessage);
}

function resetGame() {
    gameStarted = false;
    score = 0;
    document.getElementById("score").innerText = "0";
    document.getElementById("typingArea").value = "";
    document.getElementById("typingArea").disabled = true;
    if (isCreator) {
        document.getElementById("startGameButton").disabled = false;
        document.getElementById("startGameButton").style.display = "block";
    }
    document.getElementById("gameText").innerHTML = textToType;
    for (const user in playerCars) {
        playerCars[user].style.left = "0%";
        document.getElementById(`timer-${user}`).innerText = "00:00";
    }
    completionTimes = {};
    if (isCreator) {
        document.getElementById("startGameButton").style.display = "block";
    }
    updateStatus("Waiting");
}

function copyRoomId() {
    const roomIdText = document.getElementById("roomIdText").innerText;
    navigator.clipboard.writeText(roomIdText).then(() => {
        alert("Room ID copied to clipboard!");
    });
}
function inviteFriend() {
    if (roomId === "") {
        alert("Room is not created yet!");
        return;
    }
    // Create an invite link using the current URL origin, pathname and the room parameter.
    const inviteLink = `${window.location.origin}${window.location.pathname}?room=${roomId}`;
    navigator.clipboard.writeText(inviteLink).then(() => {
        alert("Invite link copied to clipboard!");
    }).catch(err => console.error(err));
}
function disableRoomButtons() {
    // Disable input fields
    document.getElementById("usernameInput").disabled = true;
    document.getElementById("roomInput").disabled = true;
    // Disable buttons
    document.getElementById("createRoomButton").disabled = true;
    document.getElementById("joinRoomButton").disabled = true;
}
connection.on("NewTextGenerated", (newText) => {
    textToType = newText;
    document.getElementById("gameText").innerText = newText;
});