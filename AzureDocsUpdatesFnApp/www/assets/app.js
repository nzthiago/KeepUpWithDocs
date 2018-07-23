/// <reference path="vue.js" />

var data = {
    loading: true,
    page: 1,
    dates: 1,
    results: 2,
    selectedProducts: "all"
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

                $(".btn-rss").attr("href", "/feed");
                data.loading = true;
                $.getJSON("/api/ChangeFeeed", function (result) {
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
            $(".btn-rss").attr("href", "/feed?products=" + query);
            data.loading = true;
            $.getJSON("/api/ChangeFeeed?products=" + query, function (result) {
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
            $.getJSON("/api/ChangeFeeed?page=" + this.page + "&date=" + getQueryStringValue("date"), function (result) {
                data.loading = false;
                data.dates = result;
            });
        }
    },
    beforeMount: function beforeMount() {
        this.load();
    }
})

function getQueryStringValue(key) {
    return decodeURIComponent(window.location.search.replace(new RegExp("^(?:.*[&\\?]" + encodeURIComponent(key).replace(/[\.\+\*]/g, "\\$&") + "(?:\\=([^&]*))?)?.*$", "i"), "$1"));
} 