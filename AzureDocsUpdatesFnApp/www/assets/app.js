/// <reference path="vue.js" />

var graphApiEndpoint = "https://graph.microsoft.com/v1.0/me";
var graphAPIScopes = ["https://graph.microsoft.com/user.read"];

var msalconfig = {
    clientID: "05cbebd2-ea3b-47c3-bcd2-ff409ad7bc38",
    redirectUri: location.origin
};

var userAgentApplication = new Msal.UserAgentApplication(msalconfig.clientID, null, loginCallback, {
    redirectUri: msalconfig.redirectUri
});

if (userAgentApplication.redirectUri) {
    userAgentApplication.redirectUri = msalconfig.redirectUri;
}

window.onload = function () {
    // If page is refreshed, continue to display user info
    if (!userAgentApplication.isCallback(window.location.hash) && window.parent === window && !window.opener) {
        var user = userAgentApplication.getUser();
        if (user) {
            callGraphApi();
        }
    }
}

function callGraphApi() {
    var user = userAgentApplication.getUser();
    if (!user) {
        userAgentApplication.loginRedirect(graphAPIScopes);
    } else {
        var userInfoElement = document.getElementById("userInfo");
        userInfoElement.parentElement.classList.remove("hidden");
        userInfoElement.innerHTML = JSON.stringify(user, null, 4);

        document.getElementById("signOutButton").classList.remove("hidden");

        var graphCallResponseElement = document.getElementById("graphResponse");
        graphCallResponseElement.parentElement.classList.remove("hidden");
        graphCallResponseElement.innerText = "Calling Graph ...";

        userAgentApplication.acquireTokenSilent(graphAPIScopes)
            .then(function (token) {
                callWebApiWithToken(graphApiEndpoint, token, graphCallResponseElement, document.getElementById("accessToken"));

            }, function (error) {
                if (error) {
                    userAgentApplication.acquireTokenRedirect(graphAPIScopes);
                }
            });

    }
}

function showError(endpoint, error, errorDesc) {
    var formattedError = JSON.stringify(error, null, 4);
    if (formattedError.length < 3) {
        formattedError = error;
    }
    document.getElementById("errorMessage").innerHTML = "An error has occurred:<br/>Endpoint: " + endpoint + "<br/>Error: " + formattedError + "<br/>" + errorDesc;
    console.error(error);
}

function loginCallback(errorDesc, token, error, tokenType) {
    if (errorDesc) {
        showError(msal.authority, error, errorDesc);
    } else {
        callGraphApi();
    }
}

function callWebApiWithToken(endpoint, token, responseElement, showTokenElement) {
    var headers = new Headers();
    var bearer = "Bearer " + token;
    headers.append("Authorization", bearer);
    var options = {
        method: "GET",
        headers: headers
    };

    fetch(endpoint, options)
        .then(function (response) {
            var contentType = response.headers.get("content-type");
            if (response.status === 200 && contentType && contentType.indexOf("application/json") !== -1) {
                response.json()
                    .then(function (data) {
                        // Display response in the page
                        console.log(data);
                        responseElement.innerHTML = JSON.stringify(data, null, 4);
                        if (showTokenElement) {
                            showTokenElement.parentElement.classList.remove("hidden");
                            showTokenElement.innerHTML = token;
                        }
                    })
                    .catch(function (error) {
                        showError(endpoint, error);
                    });
            } else {
                response.json()
                    .then(function (data) {
                        // Display response as error in the page
                        showError(endpoint, data);
                    })
                    .catch(function (error) {
                        showError(endpoint, error);
                    });
            }
        })
        .catch(function (error) {
            showError(endpoint, error);
        });
}

function signOut() {
    userAgentApplication.logout();
}

var data = {
    loading: true,
    page: 1,
    dates: 1,
    results: 2,
    selectedProducts: "all",
    productMap:null
};

function remove(array, element) {
    const index = array.indexOf(element);
    array.splice(index, 1);
}

var app = new Vue({
    el: '#app',
    data: data, 
    watch: {
        selectedProducts: function (newVal, oldVal) {
            if (newVal == oldVal
                || !Array.isArray(newVal)) {
                return;
            }
            if (this.selectedProducts == ""
                || (newVal.indexOf("all") > -1
                    && oldVal.indexOf("all") == -1)) {

                // All selected
                this.selectedProducts = "all";
                $('#productfilter').find('option').prop('selected', false);
                $('#productfilter').find('option[value=all]').prop('selected', true);
                $('#productfilter').selectpicker('refresh');

                $(".btn-rss").attr("href", "https://keepingupwithdocs.azurewebsites.net/feed");
                data.loading = true;
                $.getJSON("https://keepingupwithdocs.azurewebsites.net/api/ChangeFeeed", function (result) {
                    data.loading = false;
                    data.dates = result;
                });

                return;
            }
            if (newVal.indexOf("all") > -1
                && oldVal.indexOf("all") > -1
                && Array.isArray(newVal)) {
                // Others selected
                remove(this.selectedProducts, "all");
                $('#productfilter').find('option[value=all]').prop('selected', false);
                $('#productfilter').selectpicker('refresh');
            }

            var query = newVal.join(',');
            $(".btn-rss").attr("href", "https://keepingupwithdocs.azurewebsites.net/feed?products=" + query);
            data.loading = true;
            $.getJSON("https://keepingupwithdocs.azurewebsites.net/api/ChangeFeeed?products=" + query, function (result) {
                data.loading = false;
                data.dates = result;
            });
        }
    },
    methods: {
        getMaps: function () {
            console.log("inside get maps");  
            $.ajax({
                url: 'https://keepingupwithdocs.azurewebsites.net/api/ProductMapping',
                method: 'GET',
                async: false,
            }).then(function (response) {
                data.productMap = response;
            }).catch(function (err) {
                console.error(err);
            });
        },
        formatDate: function formatDate(date) {
            date = new Date(date);

            var monthNames = [
                "January", "February", "March",
                "April", "May", "June", "July",
                "August", "September", "October",
                "November", "December"
            ];

            var day = date.getDate();
            var monthIndex = date.getMonth();

            return day + '. ' + monthNames[monthIndex];
        },
        load: function formatDate() {
            this.page = getQueryStringValue("page");
            if (!this.page) {
                this.page = 1;
            }

            data.loading = true;
            $.getJSON("https://keepingupwithdocs.azurewebsites.net/api/ChangeFeeed?page=" + this.page + "&date=" + getQueryStringValue("date"), function (result) {
                data.loading = false;
                data.dates = result;
            });
        }
    },
    created: function created() {
        this.getMaps();
    },
    beforeMount: function beforeMount() {
        this.load();
    }
})

function getQueryStringValue(key) {
    return decodeURIComponent(window.location.search.replace(new RegExp("^(?:.*[&\\?]" + encodeURIComponent(key).replace(/[\.\+\*]/g, "\\$&") + "(?:\\=([^&]*))?)?.*$", "i"), "$1"));
} 