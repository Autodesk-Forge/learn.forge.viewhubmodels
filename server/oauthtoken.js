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

// web framework
var express = require('express');
var router = express.Router();

// Forge NPM
var forgeSDK = require('forge-apis');

// Forge config information, such as client ID and secret
var config = require('./config');

// actually perform the token operation
var oauth = require('./oauth');

router.get('/api/forge/callback/oauth', function (req, res) {
    var code = req.query.code;
    var credentials = new oauth(req.session);
    credentials.setCode(code).then(function () {
        res.redirect("/");
    }).catch(function (error) {
        res.end(JSON.stringify(error));
    });
});

router.get('/api/forge/oauth/url', function (req, res) {
    var url =
        "https://developer.api.autodesk.com" +
        '/authentication/v1/authorize?response_type=code' +
        '&client_id=' + config.credentials.client_id +
        '&redirect_uri=' + config.credentials.callback_url +
        '&scope=' + config.scopeInternal.join(" ");
    res.end(url);
});

router.get('/api/forge/oauth/signout', function (req, res) {
    req.session = null;
    res.redirect("/");
});

// Endpoint to return a 2-legged access token
router.get('/api/forge/oauth/token', function (req, res) {
    var credentials = new oauth(req.session);
    if (!credentials.isAuthorized()) {
        res.status(401).end();
        return;
    }

    credentials.getTokenPublic()
        .then(function (accessToken) {
            res.json(accessToken);
        })
        .catch(function () {
            res.status(500).end();
        })
});

module.exports = router;