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

var express = require('express');
var app = express();

// prepare server routing
app.use('/', express.static(__dirname + '/../www')); // redirect static calls
app.set('port', process.env.PORT || 3000); // main port

// cookie-based session
var cookieSession = require('cookie-session')
app.use(cookieSession({
    name: 'forgesession',
    keys: ['forgesecurekey'],
    secure: (process.env.NODE_ENV == 'production'),
    maxAge: 14 * 24 * 60 * 60 * 1000 // 14 days, same as refresh token
}))

// prepare our API endpoint routing
loadRoute('./oauthtoken');
// viewmodels sample
loadRoute('./oss');
loadRoute('./modelderivative1');
// view hub models sample
loadRoute('./datamanagement');
loadRoute('./user');

function loadRoute(path) {
    try {
        require.resolve(path);
        var m = require(path);
        app.use('/', m);
    } catch (e) { }
}

module.exports = app;