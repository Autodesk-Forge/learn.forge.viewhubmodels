const { AuthClientThreeLegged } = require('forge-apis');

const config = require('../../config');

class OAuth {
    constructor(session) {
        this._session = session;
    }

    getClient(scopes = config.scopes.internal) {
        const { client_id, client_secret, callback_url } = config.credentials;
        return new AuthClientThreeLegged(client_id, client_secret, callback_url, scopes);
    }

    isAuthorized() {
        return !!this._session.public_token;
    }

    async getPublicToken() {
        if (this._isExpired()) {
            await this._refreshTokens();
        }

        return {
            access_token: this._session.public_token,
            expires_in: this._expiresIn()
        };
    }

    async getInternalToken() {
        if (this._isExpired()) {
            await this._refreshTokens();
        }

        return {
            access_token: this._session.internal_token,
            expires_in: this._expiresIn()
        };
    }

    // On callback, pass the CODE to this function, it will
    // get the internal and public tokens and store them 
    // on the session
    async setCode(code) {
        const internalTokenClient = this.getClient(config.scopes.internal);
        const publicTokenClient = this.getClient(config.scopes.public);
        const internalCredentials = await internalTokenClient.getToken(code);
        const publicCredentials = await publicTokenClient.refreshToken(internalCredentials);

        const now = new Date();
        this._session.internal_token = internalCredentials.access_token;
        this._session.public_token = publicCredentials.access_token;
        this._session.refresh_token = publicCredentials.refresh_token;
        this._session.expires_at = (now.setSeconds(now.getSeconds() + publicCredentials.expires_in));
    }

    _expiresIn() {
        const now = new Date();
        const expiresAt = new Date(this._session.expires_at)
        return Math.round((expiresAt.getTime() - now.getTime()) / 1000);
    };

    _isExpired() {
        return (new Date() > new Date(this._session.expires_at));
    }

    async _refreshTokens() {
        let internalTokenClient = this.getClient(config.scopes.internal);
        let publicTokenClient = this.getClient(config.scopes.public);
        const internalCredentials = await internalTokenClient.refreshToken({ refresh_token: this._session.refresh_token });
        const publicCredentials = await publicTokenClient.refreshToken(internalCredentials);

        const now = new Date();
        this._session.internal_token = internalCredentials.access_token;
        this._session.public_token = publicCredentials.access_token;
        this._session.refresh_token = publicCredentials.refresh_token;
        this._session.expires_at = (now.setSeconds(now.getSeconds() + publicCredentials.expires_in));
    }
}

module.exports = { OAuth };
