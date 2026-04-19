# Bugay.Initialize.Docker

The purpose of this project is to help eliminate the need for Docker Desktop. Docker Desktop is a resource hog. I would
rather control my own destiny and install and setup docker cli myself.

## How it works

It is a Windows Service that runs on startup. It will launch WSL in the background so that the Host OS (Windows) can 
communicate with the Docker engine running in WSL.

