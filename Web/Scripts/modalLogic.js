// hook 'show' event for ALL bootstrap modals so we can ensure no 2 modals are open simultaneously

$('.modal').on('show.bs.modal', function () {
	$('.modal').not($(this)).each(function () {
		$(this).modal('hide');
	});
});

/* For every Bootstrap modal's buttons, attach a keydown that when triggered checks if it's the first or last button. */
/* This is a less general solution because we have multiple .modal-content elements in the DOM, and buttons go in and out of visibility. */
/* Handles minicart, miniwish, and session modal */

$('.modal-content :button, .switch-mini-link').on('keydown', function (e) {

	// Only handle tab key
	if(e.which !== 9)
		return;

	var ariaVisibleTabElements = $('.modal-content :button:visible, .switch-mini-link:visible');
	if(!e.shiftKey && $(this).is(ariaVisibleTabElements.last())) {
		$(this).closest('.modal-content').find(ariaVisibleTabElements.first()).focus();
		return false;
	}

	if(e.shiftKey && $(this).is(ariaVisibleTabElements.first())) {
		$(this).closest('.modal-content').find(ariaVisibleTabElements.last()).focus();
		return false;
	}

});

/* Modal windows opened by JS triggered on a button click don't get the relatedTarget property set, so focus doesn't automatically go back to the clicked element */

$('.js-add-to-cart-button, .js-wishlist-button').on('click', function () {
	var clickedElementId = $(this).attr('id');

	$('.modal').on('shown.bs.modal', function () {
		$(this).on('hidden.bs.modal', function () {
			$('#' + clickedElementId).focus();
		})
	})
});
