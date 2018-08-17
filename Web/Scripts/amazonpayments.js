var AdnsfAmazonPayments = (function () {
	return {
		initClientId: function (clientId) {
			window.onAmazonLoginReady = function () {
				amazon.Login.setClientId(clientId);
			};
		},

		checkoutButton: function (scriptTarget, merchantId, callbackEndpoint) {
			OffAmazonPayments.Button(
				scriptTarget,
				merchantId, {
					type: 'PwA',
					color: 'Gold',
					size: 'small',
					authorization: function () {
						amazon.Login.authorize({
							scope: 'profile payments:widget payments:shipping_address'
						},
							callbackEndpoint);
					}
				});
		},

		loginButton: function (merchantId, callbackEndpoint) {
			OffAmazonPayments.Button(
				"AmazonPayButton",
				merchantId, {
					authorization: function () {
						amazon.Login.authorize({
							scope: "profile payments:widget payments:shipping_address"
						},
							callbackEndpoint);
					},
					onError: function (error) {
						alert(error.getErrorCode() + ": " + error.getErrorMessage());
					}
				});
		},

		addressBook: function (amazonOrderReferenceId, merchantId, clearAmazonPaymentsUrl) {
			var addressSelected = false;
			var walletSelected = false;

			var isAmazonFormsValid = function () {
				return addressSelected && walletSelected;
			};

			new OffAmazonPayments.Widgets.AddressBook({
				amazonOrderReferenceId: amazonOrderReferenceId,
				sellerId: merchantId,
				displayMode: 'Edit',
				design: {
					designMode: 'responsive'
				},
				onOrderReferenceCreate: function (amazonOrderReference) {
					$("#AmazonOrderReferenceId").val(amazonOrderReference.getAmazonOrderReferenceId());
				},
				onAddressSelect: function () {
					addressSelected = true;
					if (isAmazonFormsValid())
						$("#submit").prop('disabled', false);
				},
				onError: function (error) {
					document.location = clearAmazonPaymentsUrl;
				}
			}).bind("amazonAddressWidget");

			new OffAmazonPayments.Widgets.Wallet({
				amazonOrderReferenceId: amazonOrderReferenceId,
				sellerId: merchantId,
				onPaymentSelect: function (orderReference) {
					walletSelected = true;
					if (isAmazonFormsValid())
						$("#submit").prop('disabled', false);
				},
				design: {
					designMode: 'responsive'
				},
				onError: function (error) {
					document.location = clearAmazonPaymentsUrl;
				}
			}).bind("walletWidgetDiv");

			$("#js-amazon-logout")
				.click(function () {
					amazon.Login.logout();
					document.location = clearAmazonPaymentsUrl;
				});
		}
	}
})(adnsf$);
