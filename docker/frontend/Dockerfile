FROM node:22-alpine

WORKDIR /workspace/frontend

COPY docker/frontend/entrypoint.sh /usr/local/bin/ism-frontend-entrypoint
RUN chmod +x /usr/local/bin/ism-frontend-entrypoint

CMD ["ism-frontend-entrypoint"]
