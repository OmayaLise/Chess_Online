# Chess Socket
## Project description ##
This project aims at creating a local (LAN) multiplayer chess based on a template. We only have to do the networking part using the socket .NET API in C# as we already had a working chess game.

## Features ##
 - [x] Create a server and a client on the host computer
 - [x] Other players can connect to the server by typing its local IP
 - [x] When the game has ended the server tells each client who won
 - [x] The initialisation and configuration of the game is made by a simple UI
 - [x] Other players can join the game as spectators at anytime

## How to use ##
+ Start the .exe file two times (one for the client/server and one for the client only)
+ Choose "Host" on one of the two instances and on the bottom left corner you should see your local IP
+ Choose "Join" on the second intance and type the IP written within the host instance
+ You can now play chess, one turn at the time on each instance (white is played by the host)
+ If you wish to join, to spectate, the current game with a third or more instance, you can do it by repeating step 3 (join the game by typing IP)

## Credit ##
Authors : Kristian GOUPIL, Omaya LISE <br>
Project started on 16-10-2023 <br>
Project ended on 27-10-2023 <br>
