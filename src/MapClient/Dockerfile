FROM nginx:alpine

COPY ./dist /usr/share/nginx/html

RUN rm -f /usr/share/nginx/html/config.json

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]