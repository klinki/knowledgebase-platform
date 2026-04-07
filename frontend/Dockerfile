FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . ./
ARG APP_VERSION=""
ARG BUILD_DATE=""
ENV APP_VERSION=$APP_VERSION
ENV BUILD_DATE=$BUILD_DATE
RUN npm run build

FROM caddy:2.10-alpine
WORKDIR /srv

COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/dist/frontend/browser ./
