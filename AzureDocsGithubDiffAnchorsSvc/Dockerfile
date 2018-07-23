FROM node:boron

# Create app directory
WORKDIR /usr/src/app

# Install app dependencies
COPY package.json .
COPY package-lock.json .
RUN npm install

# Bundle app source
COPY . .

# Build to latest 
RUN npm run build

EXPOSE 80
CMD [ "npm", "start" ]
