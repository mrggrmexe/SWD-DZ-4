# syntax=docker/dockerfile:1

FROM node:20-alpine AS build
WORKDIR /app

# Copy only manifests first for caching
COPY src/Frontend/package.json src/Frontend/package-lock.json ./
RUN npm ci

# Copy frontend sources
COPY src/Frontend/ ./

RUN npm run build

FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html

# Correct path (because build context is repo root)
COPY src/Frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built files
COPY --from=build /app/dist ./

EXPOSE 80
