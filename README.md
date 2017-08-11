# team-ssh
Do you have multiple devices out in the field that may be sitting behind NAT and you want to SSH into them?

This is proof of concept software for server-side and client-side components to implement a TeamViewer-like functionality to tunnel 
connections between 2 ends that can both connect to the server.  I talk about SSH, as I feel it is the most useful use, but this software
can be used for any TCP connection between any two endpoints.

## Components
### TeamSSHWebService
Publish the project to your web server that supports aspnetcore 1.1 and has websockets enabled.  I use it in an Azure virtual application
and it works fine.
### TeamSSHClient
This runs on your endpoints and implements the client/server functionality and you must have dotnetcore 1.1 installed for this run.
#### SSH Server
    dotnet run register --suri ws://myserver.com --lport 22
    dotnet run server
#### Client
    dotnet run add --suri ws://myserver.com --lport 10022
    dotnet run client
You can now use a terminal to connect to the remote ssh via the local port:

    ssh -p 10022 localhost
Or you can use any SSH software such as Putty.

If you have multiple devices you need to assign them different connection ids.
#### SSH Server
##### Device 1
    dotnet run register --suri ws://myserver.com --lport 22 --id 1
    dotnet run server
##### Device 2
    dotnet run register --suri ws://myserver.com --lport 22 --id 2
    dotnet run server
#### Client
    dotnet run add --suri ws://myserver.com --lport 10022 --id 1
    dotnet run add --suri ws://myserver.com --lport 10023 --id 2
    dotnet run client
Now use a SSH via port 10022 to connect to device 1, and 10023 to connect to device 2.
## Roadmap
This is obviously early days for this project and most of the functionality is in place to determine if the idea is practical.  I have
no intention myself of setting up a website that anyone can use to register their devices, I just plan on using it for my own purposes
but I put it up here so other people can use it if they want.
### Stuff I Still Need to Do
* Add public/private keying so that only devices with my key can connect to my server.
* Add webservice connection id generation so a device can register with the webservice and get an ID that can be used by any client
to connect to it, like TeamViewer works.
* Investigate setting server up as a linux systemctl process so that it is always running on linux.
* Investigate setting server up as a windows service so that it is always running on windows.
* Add views to the web service to get information on registered and connected devices.
