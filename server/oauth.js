/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

'use strict';

// Forge NPM
var forgeSDK = require('forge-apis');

// Forge config information, such as client ID and secret
var config = require('./config');

function OAuth(session) {
    this._session = session;
};

// returns the Public scope token (Viewer)
OAuth.prototype.getTokenPublic = function () {
    var _this = this;
    return new Promise(function (resolve, reject) {
        if (_this.isExpired())
            _this.refreshToken().then(function () {
                resolve({ access_token: _this._session.tokenPublic, expires_in: _this.getExpiresIn() })
            });
        else
            resolve({ access_token: _this._session.tokenPublic, expires_in: _this.getExpiresIn() })
    });
};

// returns the Internal scope token (data management)
OAuth.prototype.getTokenInternal = function () {
    var _this = this;
    return new Promise(function (resolve, reject) {
        if (_this.isExpired())
            _this.refreshToken().then(function () {
                resolve({ access_token: _this._session.tokenInternal, expires_in: _this.getExpiresIn() })
            });
        else
            resolve({ access_token: _this._session.tokenInternal, expires_in: _this.getExpiresIn() })
    });
};

OAuth.prototype.getExpiresIn = function () {
    var now = new Date();
    var expiresAt = new Date(this._session.expiresAt)
    return Math.round((expiresAt.getTime() - now.getTime()) / 1000);
};

OAuth.prototype.isExpired = function () {
    return (new Date() > new Date(this._session.expiresAt))
};

OAuth.prototype.isAuthorized = function () {
    // !! converts value into boolean
    return (!!this._session.tokenPublic);
};

// On callback, pass the CODE to this function, it will
// get the internal and public tokens and store them 
// on the session
OAuth.prototype.setCode = function (code) {
    var forgeOAuthInternal = this.OAuthClient(config.scopeInternal);
    var forgeOAuthPublic = this.OAuthClient(config.scopePublic);
    var _this = this;

    return new Promise(function (resolve, reject) {
        forgeOAuthInternal.getToken(code)
            .then(function (credentialsInternal) {
                forgeOAuthPublic.refreshToken(credentialsInternal)
                    .then(function (credentialsPublic) {
                        _this._session.tokenInternal = credentialsInternal.access_token;
                        _this._session.tokenPublic = credentialsPublic.access_token;
                        _this._session.refreshToken = credentialsPublic.refresh_token;
                        var now = new Date();
                        _this._session.expiresAt = (now.setSeconds(now.getSeconds() + credentialsPublic.expires_in));
                        resolve();
                    })
                    .catch(function (error) {
                        console.log('Error at OAuth refreshToken:');
                        console.log(error);
                        reject(error)
                    });
            })
            .catch(function (error) {
                console.log('Error at OAuth getToken:');
                console.log(error);
                reject(error)
            });
    })
}

// refresh both internal and public tokens, keep new refresh token
OAuth.prototype.refreshToken = function () {
    var forgeOAuthInternal = this.OAuthClient(config.scopeInternal);
    var forgeOAuthPublic = this.OAuthClient(config.scopePublic);
    var _this = this;

    return new Promise(function (resolve, reject) {
        forgeOAuthInternal.refreshToken({ refresh_token: _this._session.refreshToken })
            .then(function (credentialsInternal) {
                forgeOAuthPublic.refreshToken(credentialsInternal)
                    .then(function (credentialsPublic) {
                        _this._session.tokenInternal = credentialsInternal.access_token;
                        _this._session.tokenPublic = credentialsPublic.access_token;
                        _this._session.refreshToken = credentialsPublic.refresh_token;
                        var now = new Date();
                        _this._session.expiresAt = (now.setSeconds(now.getSeconds() + credentialsPublic.expires_in));
                        resolve();
                    })
                    .catch(function (error) {
                        console.log('Error at OAuth refreshToken public');
                        console.log(error);
                        reject(error)
                    });
            })
            .catch(function (error) {
                console.log('Error at OAuth refreshToken internal:');
                console.log(error);
                reject(error)
            });
    });
}

OAuth.prototype.OAuthClient = function (scopes) {
    var client_id = config.credentials.client_id;
    var client_secret = config.credentials.client_secret;
    var callback_url = config.credentials.callback_url;
    if (scopes == undefined) scopes = config.scopeInternal;
    return new forgeSDK.AuthClientThreeLegged(client_id, client_secret, callback_url, scopes);
}

module.exports = OAuth;