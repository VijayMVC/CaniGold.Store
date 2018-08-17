// Configure jQuery validation to use Bootstrap classes
(function ($) {
	var defaultOptions = {
		errorClass: 'has-error',
		validClass: 'has-success',
		highlight: function (element, errorClass, validClass) {
			$(element)
				.closest('.form-group')
				.addClass(errorClass)
				.removeClass(validClass);
		},
		unhighlight: function (element, errorClass, validClass) {
			$(element)
				.closest('.form-group')
				.removeClass(errorClass)
				.addClass(validClass);
		}
	};

	$.validator.setDefaults(defaultOptions);

	// Set focus to first form field with an error rather than using "findLastActive()"
	$.validator.prototype.focusInvalid = function () {
		if (this.settings.focusInvalid) {
			try {
				$(this.errorList.length && this.errorList[0].element || [])
					.filter(":visible")
					.focus()
					// manually trigger focusin event; without it, focusin handler isn't called, findLastActive won't have anything to find
					.trigger("focusin");
			} catch (e) {
				// ignore IE throwing errors when focusing hidden elements
			}
		}
	};

	$.validator.unobtrusive.options = {
		errorClass: defaultOptions.errorClass,
		validClass: defaultOptions.validClass,
	};
})(jQuery);
