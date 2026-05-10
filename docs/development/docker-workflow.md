# Docker Workflow

- `frontend`, `backend` and `mysql` run as isolated services
- Source code is mounted into the containers with bind mounts
- Backend uses `dotnet watch run`
- Frontend uses `nuxt dev`
- Backend container applies EF migrations before starting the watcher
