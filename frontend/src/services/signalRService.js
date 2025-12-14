import * as signalR from '@microsoft/signalr';

class SignalRService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.connectionPromise = null;
        this.listeners = new Map();
    }

    async startConnection(token) {
        // If already connected, return
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            console.log('SignalR already connected');
            return;
        }

        if (this.connectionPromise) {
            return this.connectionPromise;
        }

        this.connectionPromise = new Promise(async (resolve, reject) => {
            try {
                // Stop existing connection if any
                if (this.connection) {
                    await this.connection.stop();
                }

                this.connection = new signalR.HubConnectionBuilder()
                    .withUrl('http://localhost:5216/gameHub', {
                        accessTokenFactory: () => token,
                        skipNegotiation: false,
                        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
                    })
                    .withAutomaticReconnect({
                        nextRetryDelayInMilliseconds: retryContext => {
                            if (retryContext.elapsedMilliseconds < 10000) {
                                return 2000;
                            } else if (retryContext.elapsedMilliseconds < 30000) {
                                return 5000;
                            } else {
                                return 10000;
                            }
                        }
                    })
                    .configureLogging(signalR.LogLevel.Information)
                    .build();

                this.attachStoredListeners();

                // Setup reconnection handlers
                this.connection.onreconnecting((error) => {
                    console.log('SignalR reconnecting:', error);
                    this.isConnected = false;
                });

                this.connection.onreconnected((connectionId) => {
                    console.log('SignalR reconnected:', connectionId);
                    this.isConnected = true;
                });

                this.connection.onclose((error) => {
                    console.log('SignalR connection closed:', error);
                    this.isConnected = false;
                    this.connectionPromise = null;
                });

                // Start the connection
                await this.connection.start();
                this.isConnected = true;
                console.log('SignalR Connected successfully, connection state:', this.connection.state);

                resolve();
            } catch (error) {
                console.error('Error starting SignalR connection:', error);
                this.isConnected = false;
                this.connectionPromise = null;
                reject(error);
            }
        });

        return this.connectionPromise;
    }



    attachStoredListeners() {
        if (!this.connection) return;

        for (const [eventName, callbacks] of this.listeners.entries()) {
            this.connection.off(eventName);
            for (const cb of callbacks) {
                this.connection.on(eventName, cb);
            }
        }
    }


    async stopConnection() {
        if (this.connection) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                this.connectionPromise = null;
                console.log('SignalR connection stopped');
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            }
        }
    }

    async joinGameRoom(roomId) {
        // Wait for connection to be established
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established. Call startConnection first.');
        }

        // Check connection state
        if (this.connection.state !== signalR.HubConnectionState.Connected) {
            throw new Error(`Cannot join room. Connection state is: ${this.connection.state}`);
        }

        try {
            await this.connection.invoke('JoinGameRoom', roomId);
            console.log(`Joined game room: ${roomId}`);
        } catch (error) {
            console.error('Error joining game room:', error);
            throw error;
        }
    }

    async leaveGameRoom(roomId) {
        if (!this.connection || !this.isConnected) {
            console.log('Cannot leave room - not connected');
            return;
        }

        try {
            await this.connection.invoke('LeaveGameRoom', roomId);
            console.log(`Left game room: ${roomId}`);
        } catch (error) {
            console.error('Error leaving game room:', error);
        }
    }

    // Event listener management (keep your existing methods)
    onPlayerJoined(callback) {
        this.registerListener('PlayerJoined', callback);
    }

    onPlayerLeft(callback) {
        this.registerListener('PlayerLeft', callback);
    }

    onGameStarted(callback) {
        this.registerListener('GameStarted', callback);
    }

    onNextQuestion(callback) {
        this.registerListener('NextQuestion', callback);
    }

    onAnswerSubmitted(callback) {
        this.registerListener('AnswerSubmitted', callback);
    }

    onGameFinished(callback) {
        this.registerListener('GameFinished', callback);
    }

    onSettingsUpdated(callback) {
        this.registerListener('SettingsUpdated', callback);
    }

    onGameStartFailed(callback) {
        this.registerListener('GameStartFailed', callback);
    }


    onGameReset(callback) {
        this.registerListener('GameReset', callback);
    }


    // Voting-related listeners
    onCategoryVotingStarted(callback) {
        this.registerListener('CategoryVotingStarted', callback);
    }

    onCategoryVoteUpdated(callback) {
        this.registerListener('CategoryVoteUpdated', callback);
    }

    onCategoryVotingFinished(callback) {
        this.registerListener('CategoryVotingFinished', callback);
    }


    onCategoryRevoteStarted(callback) {
        this.registerListener('CategoryRevoteStarted', callback);
    }

    onCategoryVotingTimer(callback) {
        this.registerListener('CategoryVotingTimer', callback);
    }

    onGameStateSync(handler) {
        this.registerListener("GameStateSync", handler);
    }

    offGameStateSync(handler) {
        this.removeListener("GameStateSync", handler);
    }
    // Generic listener registration


    registerListener(eventName, callback) {
        if (!this.listeners.has(eventName)) {
            this.listeners.set(eventName, []);
        }

        const arr = this.listeners.get(eventName);
        if (!arr.includes(callback)) {
            arr.push(callback);
        }

        if (this.connection) {
            this.connection.on(eventName, callback);
            console.log(`Registered listener for: ${eventName}`);
        } else {
            console.log(`Stored listener for later (no connection yet): ${eventName}`);
        }
    }



    // Remove a specific listener

    removeListener(eventName, callback) {
        if (!this.connection) {
            // Still clear stored listeners even if not connected
            if (!callback) {
                this.listeners.delete(eventName);
            } else if (this.listeners.has(eventName)) {
                const arr = this.listeners.get(eventName);
                const idx = arr.indexOf(callback);
                if (idx > -1) arr.splice(idx, 1);
                if (arr.length === 0) this.listeners.delete(eventName);
            }
            return;
        }

        try {
            if (!callback) {
                // remove ALL handlers for this event
                this.connection.off(eventName);
                this.listeners.delete(eventName);
                console.log(`Removed ALL listeners for: ${eventName}`);
                return;
            }

            this.connection.off(eventName, callback);

            if (this.listeners.has(eventName)) {
                const arr = this.listeners.get(eventName);
                const idx = arr.indexOf(callback);
                if (idx > -1) arr.splice(idx, 1);
                if (arr.length === 0) this.listeners.delete(eventName);
            }

            console.log(`Removed listener for: ${eventName}`);
        } catch (error) {
            console.error(`Error removing listener for ${eventName}:`, error);
        }
    }


    // Remove all listeners for an event
    removeAllListeners(eventName) {
        if (!this.connection) {
            return;
        }

        try {
            if (eventName) {
                this.connection.off(eventName);
                this.listeners.delete(eventName);
            } else {
                // Remove all listeners
                this.listeners.forEach((_, event) => {
                    this.connection.off(event);
                });
                this.listeners.clear();
            }
            console.log(`Removed all listeners${eventName ? ` for: ${eventName}` : ''}`);
        } catch (error) {
            console.error('Error removing listeners:', error);
        }
    }

    // Hub method invocations
    async startGame(roomId) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('StartGame', roomId);
        } catch (error) {
            console.error('Error starting game:', error);
            throw error;
        }
    }

    async submitAnswer(roomId, questionId, selectedAnswers) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('SubmitAnswer', roomId, questionId, selectedAnswers);
        } catch (error) {
            console.error('Error submitting answer:', error);
            throw error;
        }
    }

    async nextQuestion(roomId) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('NextQuestion', roomId);
        } catch (error) {
            console.error('Error advancing to next question:', error);
            throw error;
        }
    }

    async updateGameSettings(roomId, settings) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('UpdateGameSettings', roomId, settings);
        } catch (error) {
            console.error('Error updating game settings:', error);
            throw error;
        }
    }

    getConnectionState() {
        if (!this.connection) {
            return 'Disconnected';
        }
        return signalR.HubConnectionState[this.connection.state];
    }

    // New method to wait for connection
    async waitForConnection() {
        if (this.isConnected) {
            return;
        }

        if (this.connectionPromise) {
            await this.connectionPromise;
        } else {
            throw new Error('Connection not started');
        }
    }


    async startCategoryVoting(roomId, categories) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('StartCategoryVoting', roomId, categories);
            console.log('StartCategoryVoting invoked', { roomId, categories });
        } catch (error) {
            console.error('Error starting category voting:', error);
            throw error;
        }
    }

    async submitCategoryVote(roomId, category) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('SubmitCategoryVote', roomId, category);
            console.log('SubmitCategoryVote invoked', { roomId, category });
        } catch (error) {
            console.error('Error submitting category vote:', error);
            throw error;
        }
    }

    async endCategoryVoting(roomId) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('EndCategoryVoting', roomId);
            console.log('EndCategoryVoting invoked', { roomId });
        } catch (error) {
            console.error('Error ending category voting:', error);
            throw error;
        }
    }


}

const signalRService = new SignalRService();
export default signalRService;
