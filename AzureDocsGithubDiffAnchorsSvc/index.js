"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
require('dotenv').config();
const Logger = require("bunyan");
const _ = require("lodash");
const NodeCache = require("node-cache");
const cheerio = require("cheerio");
const rp = require("request-promise");
const bluebird_1 = require("bluebird");
const restify = require("restify");
const log = new Logger({
    name: 'AzureDocsGithubDiffAnchorsSvc',
    streams: [
        {
            stream: process.stdout,
            level: 'info'
        }
    ],
    serializers: {
        req: Logger.stdSerializers.req,
        res: restify.bunyan.serializers.res
    },
});
function createErrorResponse(err) {
    const errorResponse = {
        status: 400,
        body: {
            errorMessage: `Invalid request: ${err}`
        }
    };
    return errorResponse;
}
function scrapGithubPageForDiffAnchors($) {
    const res = {};
    $('.file-header').each((i, elem) => {
        const fileName = $(elem).data('path');
        const anchor = $(elem).data('anchor');
        if (_.isString(fileName) && _.isString(anchor) && !_.isEmpty(fileName) && !_.isEmpty(anchor)) {
            res[fileName] = anchor.replace(/^(diff-)/, '');
        }
    });
    return res;
}
function getCommitDiffAnchors(githubDiffsUrl, diffAnchors) {
    const options = {
        uri: githubDiffsUrl,
        transform: function (body) {
            return cheerio.load(body);
        }
    };
    return rp(options)
        .then(($) => {
        // Results start to overlap with paging (really strange...). But this is the stop criterium.
        const diffAnchorsRes = scrapGithubPageForDiffAnchors($);
        const existingKeys = _.keys(diffAnchors);
        const newKeys = _.keys(diffAnchorsRes);
        let abortRecursion = false;
        _.forEach(newKeys, (nk) => {
            abortRecursion = _.has(diffAnchors, nk);
            diffAnchors[nk] = diffAnchorsRes[nk];
        });
        if (!abortRecursion) {
            let newUrl = githubDiffsUrl;
            if (githubDiffsUrl.match(/^(.*start_entry=)(\d+)$/)) {
                // replace page offset
                newUrl = githubDiffsUrl.replace(/^(.*start_entry=)(\d+)$/, `$1${_.keys(diffAnchors).length}`);
            }
            else {
                newUrl = `${githubDiffsUrl}&start_entry=${_.keys(diffAnchors).length}`;
            }
            return getCommitDiffAnchors(newUrl, diffAnchors);
        }
        else {
            return;
        }
    });
}
function isValidGithubDiffUrl(githubDiffUrl) {
    const errors = [];
    // https://github.com/MicrosoftDocs/azure-docs/diffs?commit=01cc8faf8469444a83c391f01c5ccb9474227534&sha1=31a91b942b02f3be982b4fdcea3af01bfab11263&sha2=01cc8faf8469444a83c391f01c5ccb9474227534
    if (!_.startsWith(githubDiffUrl, 'https://github.com')) {
        errors.push('[isValidGithubDiffUrl] No github.com address.');
    }
    else {
        const matches = githubDiffUrl.match(/https:\/\/github.com\/[^/]+\/[^/]+\/diffs.*/);
        if (matches.length !== 1) {
            errors.push('[isValidGithubDiffUrl] Missing URL segments.');
        }
    }
    return errors;
}
function getDiffDeepLinksFromDiffUrl(diffUrl) {
    const validationErrors = isValidGithubDiffUrl(diffUrl);
    const diffAnchors = {};
    if (validationErrors.length <= 0) {
        return getCommitDiffAnchors(diffUrl, diffAnchors)
            .then(() => {
            const res = {
                diffUrl: diffUrl,
                fileAnchorMap: diffAnchors
            };
            return res;
        });
    }
    else {
        return bluebird_1.Promise.reject(validationErrors.join('\n'));
    }
}
const port = process.env.port || process.env.PORT || 80;
const server = restify.createServer({ log: log });
server.use(restify.plugins.queryParser());
server.pre(function (request, response, next) {
    request.log.info({ req: request }, 'start');
    return next();
});
server.on('after', function (req, res, route) {
    req.log.info({ res: res }, 'end');
});
const oneMonthTTL = 60 * 60 * 24 * 30;
const cache = new NodeCache({ stdTTL: oneMonthTTL, deleteOnExpire: true });
server.get('/api/get-diff-anchors-for-commit', (req, res, next) => {
    if (_.isUndefined(req.query.diffUrl)) {
        res.send(400, createErrorResponse('missing "diffUrl" field in query string'));
        next();
    }
    else {
        const diffUrl = req.query.diffUrl;
        const cachedValue = cache.get(diffUrl);
        if (_.isUndefined(cachedValue)) {
            getDiffDeepLinksFromDiffUrl(diffUrl)
                .then((deepLinksRes) => {
                cache.set(diffUrl, deepLinksRes);
                const oneYear = 31536000;
                res.header('Cache-Control', `max-age=${oneYear}`);
                res.header('Expires', new Date(Date.now() + oneYear).toUTCString());
                res.send(200, deepLinksRes);
                next();
            }, (err) => {
                res.send(400, createErrorResponse(err));
                next();
            });
        }
        else {
            const oneYear = 31536000;
            res.header('Cache-Control', `max-age=${oneYear}`);
            res.header('Expires', new Date(Date.now() + oneYear).toUTCString());
            res.send(200, cachedValue);
        }
    }
});
server.listen(port);
log.info(`${server.name} listening to ${server.url}/api/get-diff-anchors-for-commit`);
log.debug(`http://127.0.0.1:${port}/api/get-diff-anchors-for-commit?diffUrl=${encodeURIComponent('https://github.com/MicrosoftDocs/azure-docs/diffs?commit=01cc8faf8469444a83c391f01c5ccb9474227534&sha1=31a91b942b02f3be982b4fdcea3af01bfab11263&sha2=01cc8faf8469444a83c391f01c5ccb9474227534')}`);
//# sourceMappingURL=index.js.map