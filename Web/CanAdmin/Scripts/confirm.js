(function ($) {
	$('a[data-confirm]').click(function (event) {
		var message = $(this).data('confirm');
		if (message && !confirm(message)) {
			event.preventDefault();
		}
	});

	$('form[data-confirm]').submit(function (event) {
		var message = $(this).data('confirm');
		if (message && !confirm(message)) {
			event.preventDefault();
		}
	});
})(jQuery);
