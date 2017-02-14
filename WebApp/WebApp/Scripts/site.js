(function ($) {
    $.fn.bootstrapValidator.validators.username = {

        validate: function (validator, $field, options) {
            var value = $field.val();
            value = $.trim(value);
            if (value == '') {
                return true;
            }

            var length = $.trim(value).length;
            if ((6 && length < 6) || (20 && length > 20)) {
                return true;
            }

            if (!isNaN(value)) return false;
            if (value.indexOf('@') != -1) return false;

            return true;
        }
    };
}(window.jQuery));