# TV Slagalica – Multiplayer Quiz Game (Server–Client)

## Authors
- [Katarina Kalauz](https://github.com/kalauzkatarina)
- [Julijana Ristić](https://github.com/julijanaristic)
---

##  Project Overview
This project is a server–client application that enables players to play three games from the popular TV quiz *Slagalica*:  
- **Slagalica** (word forming)  
- **Skočko** (symbol combination guessing)  
- **Ko zna zna** (multiple-choice questions)

Up to **two players** can connect to the same server and compete.  
The system also supports **training mode**, where a single client can connect and choose which games to play.

The server controls the game flow, validates all responses, assigns points to each player, and determines the winner at the end.

---

## Technical Description

### Communication
- **UDP** is used for player registration.
- **TCP** is used for sending questions, receiving answers, and controlling game flow.

### Server Responsibilities
- Accepts registrations from one or two players via UDP.
- Starts a full game session for two players or a training session for one.
- Sends all game data to clients (letters, combinations, questions).
- Processes answers and calculates points.
- Tracks the current game, attempts, and cumulative score.
- At the end, displays scoreboard and sends results (winner/loser) to clients.

### Client Responsibilities
- Sends a registration message via UDP:
```bash
PRIJAVA: [nickname], [sl,sk,kzz]
```
- Connects to the server's TCP port after receiving connection info.
- Sends **SPREMAN** (“READY”) when ready to begin.
- Actively participates in each game by submitting answers.

---

## Games

### Slagalica
- Server generates **12 random letters** (max 4 vowels).
- Player submits the longest valid word they can form.
- Word is validated on the server.
- **Score:** `5 × number of letters` (if valid).

### Skočko
- Server generates a secret combination of **4 symbols** from:  
`H, T, P, K, S, Z`
- Player sends guesses one by one.
- Server responds with:
- symbols in correct position  
- symbols present but in wrong position  
- symbols not present
- **Scoring based on attempt number:**  
`30, 25, 20, 15, 10, 10`

### Ko zna zna
- Server loads questions from a text file.
- Each question includes 3 possible answers.
- Player submits the number of the correct answer.
- **+10 points** for correct, **–5 points** for incorrect.

---

## Multiplayer Support
Server uses **socket multiplexing** (polling) to handle multiple TCP clients at the same time.

### Scoring Rules (when speed matters)
- First correct answer → full points  
- Second correct answer → **15% fewer points**  
*(Does not apply to Skočko)*

---

## Winner Determination
- Winner = player with **highest total score**  
- If tied → winner is the one with **more points in Skočko**

---

## Tasks Summary
1. Diagram: server–client communication flow  
2. UDP registration handling  
3. `Player` class implementation  
4. TCP game initialization with **SPREMAN**  
5. Implementation of *Slagalica*  
6. Implementation of *Skočko*  
7. Implementation of *Ko zna zna*  
8. Polling model for multiple TCP clients  
9. Scoring rules + winner logic  
10. Finalized project diagram  

---

## Example Usage
- Up to two clients connect to the same server.
- Games are played in order, points are accumulated.
- Server prints the final scoreboard.
- Clients receive a message declaring **winner** or **loser**.

---

## Technologies Used
- C (sockets, networking)
- UDP & TCP communication
- Polling model (non-blocking I/O)
- File handling for quiz questions

---

## Contact
For any questions regarding the project, feel free to contact any team member listed above.
