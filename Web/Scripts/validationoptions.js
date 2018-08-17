adnsf$.validator.setDefaults({
	submitHandler: function (form) {
		adnsf$(form).trigger('adnsfFormValidated', this.submitButton);
		return true;
	}
});