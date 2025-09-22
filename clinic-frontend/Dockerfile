

# --- build stage ---
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . ./
# Build prod bundle
RUN npx ng build --configuration production

# --- serve via nginx ---
FROM nginx:alpine
# Copy our nginx config from the build context (this folder)
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Copy Angular dist (adjust path if your project name differs)
COPY --from=build /app/dist/clinic-frontend/browser/ /usr/share/nginx/html/
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
