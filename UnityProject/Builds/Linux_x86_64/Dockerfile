FROM ubuntu:22.04
RUN apt-get update && apt-get install -y libglu1-mesa && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY . .
RUN chmod +x /app/Terraria.x86_64
EXPOSE 7777/udp
ENTRYPOINT ["/app/Terraria.x86_64", "-logfile"]