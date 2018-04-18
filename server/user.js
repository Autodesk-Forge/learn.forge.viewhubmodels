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

router.get('/api/forge/user/profile', function (req, res) {
    var credentials = new oauth(req.session);
    credentials.getTokenInternal().then(function (tokenInternal) {
        var user = new forgeSDK.UserProfileApi();
        user.getUserProfile(credentials.OAuthClient(), tokenInternal)
            .then(function (profile) {
                res.json({
                    name: profile.body.firstName + ' ' + profile.body.lastName,
                    picture: profile.body.profileImages.sizeX40
                });
            })
            .catch(function (error) {
                console.log(error);
                res.status(401).end()
            })
    });
});

module.exports = router;