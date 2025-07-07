# TerminalChatDemo

Solution demo for terminal chat with web socket communication between server and clients.

## How to send messages with clients

Chat clients can send messages each other by broadcasting message to everyone just typin message and hit enter.
They can also send private messages, by typing first client id, e.g. if I have clients with id's 1, 2, 3 I can sent message from client 2 to 3
by typing "3: hello!", or if there are multpile clients 1, 2, 3, 4 and I wanted to send messge from 1 to 2 and 4, I will type "2,4: Hello 2 and 4!".

# Run applications in VS Code:

- Open the folder WebSocketConsoleApp
- Open an integrated terminal

## Run simple server app

dotnet run --project SimpleServerApp

## Run client app

dotnet run --project ClientApp

## Run multi client supported server app

dotnet run --project ServerApp
