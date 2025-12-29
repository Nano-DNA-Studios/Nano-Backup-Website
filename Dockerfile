# Use the Dotnet Image and the Ubuntu Image to build the container
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-dist
FROM ubuntu:24.04

# Copy dotnet from the official image to your Ubuntu image
COPY --from=dotnet-dist /usr/share/dotnet /usr/share/dotnet
RUN ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

# Install dependencies first
RUN apt-get update && apt-get install -y \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# Define the Username and Dotnet Version
ENV USERNAME=Nano-Backup

# Add a new Nano-Backup User and add to Sudoers
RUN useradd -ms /bin/bash ${USERNAME} && \
usermod -aG sudo ${USERNAME} && echo "${USERNAME} ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers

# Set the Working Directory
WORKDIR /Nano-Backup

# Set environment variables so dotnet is accessible
ENV DOTNET_ROOT="/usr/share/dotnet"
ENV PATH="/usr/share/dotnet:$PATH"

# Ensure DOTNET is in global environment
RUN echo "export DOTNET_ROOT=/usr/share/dotnet" >> /etc/profile
RUN echo "export PATH=$PATH:/usr/share/dotnet" >> /etc/profile
RUN echo "export DOTNET_ROOT=/usr/share/dotnet" >> /etc/environment
RUN echo "export PATH=$PATH:/usr/share/dotnet" >> /etc/environment

# Set the Working Directory to the Action Runner Directory
WORKDIR /Nano-Backup

# Copy the Exported Application to the Container
COPY ./Nano-Backup-Website/bin/Release/net8.0/linux-x64/publish/ .

# Switch to the User
USER Nano-Backup

# Run the Website
CMD ["/bin/sh", "-c", "/Nano-Backup/Nano-Backup-Website"]