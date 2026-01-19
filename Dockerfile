# Use the Dotnet Image and the Ubuntu Image to build the container
FROM ubuntu:24.04

# Install dependencies first
RUN apt-get update && apt-get install -y \
    libicu-dev \
    p7zip \
    && rm -rf /var/lib/apt/lists/*

# Define the Username and Dotnet Version
ENV USERNAME=Nano-Backup

# Add a new Nano-Backup User and add to Sudoers
RUN useradd -ms /bin/bash ${USERNAME} && \
usermod -aG sudo ${USERNAME} && echo "${USERNAME} ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers

# Set the Working Directory
WORKDIR /Nano-Backup

# Set the Working Directory to the Action Runner Directory
WORKDIR /Nano-Backup

# Copy the Exported Application to the Container
COPY ./Nano-Backup-Website/bin/Release/net8.0/linux-x64/publish/ .

# Switch to the User
USER Nano-Backup

# Run the Website
CMD ["/bin/sh", "-c", "/Nano-Backup/Nano-Backup-Website"]