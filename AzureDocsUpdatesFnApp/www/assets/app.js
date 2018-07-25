/// <reference path="vue.js" />

var data = {
    loading: true,
    page: 1,
    dates: 1,
    results: 2,
    selectedProducts: "all",
    productMap: [{ key: "active-directory-b2c", value: "Active Directory B2c" },
    { key: "active-directory-domain-services", value: "Active Directory Domain Services" },
    { key: "active-directory-ds", value: "Active Directory Ds" },
    { key: "active-directory", value: "Active Directory" },
    { key: "advisor", value: "Advisor" },
    { key: "aks", value: "Azure Container Service (AKS)" },
    { key: "analysis-services", value: "Analysis Services" },
    { key: "ansible", value: "Ansible in Azure" },
    { key: "api-management", value: "API Management documentation" },
    { key: "app-service-mobile", value: "App Service Mobile" },
    { key: "app-service", value: "Web Apps" },
    { key: "application-gateway", value: "Application Gateway" },
    { key: "application-insights", value: "Application Insights" }]
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

                $(".btn-rss").attr("href", "https://keepupdocsfunctionapp.azurewebsites.net/feed");
                data.loading = true;
                $.getJSON("https://keepupdocsfunctionapp.azurewebsites.net/api/ChangeFeeed", function (result) {
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
            $(".btn-rss").attr("href", "https://keepupdocsfunctionapp.azurewebsites.net/feed?products=" + query);
            data.loading = true;
            $.getJSON("https://keepupdocsfunctionapp.azurewebsites.net/api/ChangeFeeed?products=" + query, function (result) {
                data.loading = false;
                data.dates = result;
            });
        }
    },
    methods: {
        
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

            $.getJSON("api/GetUserProfileByEmailAddress?emailAddress=mthapa@indiana.edu", function (result) {
                data.loading = false;
                data.selectedProducts = result.notificationProfile.categories;
            });

            data.loading = true;
            $.getJSON("https://keepupdocsfunctionapp.azurewebsites.net/api/ChangeFeeed?page=" + this.page + "&date=" + getQueryStringValue("date"), function (result) {
                data.loading = false;
                data.dates = result;
            });
        }
    },
    created: function() {
        var _this = this;
        _this.loading = true;
         $.getJSON("https://keepupdocsfunctionapp.azurewebsites.net/api/ProductMapping", function(result) {
             _this.loading = false;
             _this.productMap = result;
            });
    },
    beforeMount: function beforeMount() {
        this.load();
    }
})

var userProfileData = {
    loading: false,
    firstName: null,
    lastName: null,
    email: null,
    selectedCategories: "",
    frequency: 0,
    productOptions: [{ key: "active-directory-b2c", value: "Active Directory B2c" },
    { key: "active-directory-domain-services", value: "Active Directory Domain Services" },
    { key: "active-directory-ds", value: "Active Directory Ds" },
    { key: "active-directory", value: "Active Directory" },
    { key: "advisor", value: "Advisor" },
    { key: "aks", value: "Azure Container Service (AKS)" },
    { key: "analysis-services", value: "Analysis Services" },
    { key: "ansible", value: "Ansible in Azure" },
    { key: "api-management", value: "API Management documentation" },
    { key: "app-service-mobile", value: "App Service Mobile" },
    { key: "app-service", value: "Web Apps"},
    { key: "application-gateway", value: "Application Gateway"},
    { key: "application-insights", value: "Application Insights"}],
    errors: []
}

var userProfile = new Vue({
    el: '#userProfile',
    data: userProfileData,
    methods: {
        getUserProfile: function () {
            var self = this;
            self.loading = true;
            $.getJSON("api/GetUserProfileByEmailAddress?emailAddress=mthapa@indiana.edu", function (result) {
                console.log(result)
                self.loading = false;
                self.firstName = result.contactProfile.firstName;
                self.lastName = result.contactProfile.lastName;
                self.email = result.contactProfile.emailAddress;
                self.frequency = result.notificationProfile.frequency;
                self.selectedCategories = result.notificationProfile.categories;
            });
        },
        load: function () {
            var _this = this;
            _this.loading = true;
             $.getJSON("https://keepupdocsfunctionapp.azurewebsites.net/api/ProductMapping", function (result) {

                _this.loading = false;
                _this.productOptions = result;
            });
        },
        upsertUserProfile: function () {
            
            if (this.firstName && this.lastName && this.email && this.selectedCategories != "" && this.frequency != 0) {
                var user = {
                    ContactProfile: { EmailAddress: this.email, FirstName: this.firstName, LastName: this.lastName },
                    NotificationProfile: { Categories: this.selectedCategories, Frequency: this.frequency, EmailAddress: this.email }
                }
                $.ajax({
                    //url: 'https://keepupdocsfunctionapp.azurewebsites.net/api/UpsertUserProfile',
                    url: '/api/UpsertUserProfile',
                    method: 'PUT',
                    data: JSON.stringify(user),
                async: false,
                }).then(function (response) {
                    window.location.href = "/index.html"
            }).catch(function(err) {
                console.error(err);
            });
            }

            this.errors = [];

            if (!this.firstName) {
                this.errors.push('First Name required.');
            }

            if (!this.lastName) {
                this.errors.push('Last Name required.');
            }

            if (!this.email) {
                this.errors.push('Email required.');
            }

            if (this.frequency == 0) {
                this.errors.push('Frequency required.');
            }

            if (this.selectedCategories == "") {
                this.errors.push("Select a product category")
            }
        }
    },
    created: function created() {
        this.load();
    },
    beforeMount: function beforeMount() {
        this.load();
        },
    mounted: function () {
        this.getUserProfile();
    }
})

function getQueryStringValue(key) {
    return decodeURIComponent(window.location.search.replace(new RegExp("^(?:.*[&\\?]" + encodeURIComponent(key).replace(/[\.\+\*]/g, "\\$&") + "(?:\\=([^&]*))?)?.*$", "i"), "$1"));
} 