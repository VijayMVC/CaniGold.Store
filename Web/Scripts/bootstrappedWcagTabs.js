(function ($) {
	var TabsController = {
		init: function ($navTabs, $tabContent) {
			this.$tabs = $navTabs.find('a');
			this.$tabContent = $tabContent;
			this.handleClick = $.proxy(this.handleClick, this);
			this.setActiveTabUsingKeys = $.proxy(this.setActiveTabUsingKeys, this);
			this.setActiveTabUsingClicks = $.proxy(this.setActiveTabUsingClicks, this);
			this.handleKeyDown = $.proxy(this.handleKeyDown, this);
			this.$tabs.on('click', this.handleClick);
			this.$tabs.on('keydown', this.handleKeyDown);
		},
		handleClick: function (event) {
			event.preventDefault();
			this.setActiveTabUsingClicks(event.target);
		},
		handleKeyDown: function (event) {
			var index = this.$tabs.index(event.target);
			if (index === -1) {
				return;
			}
			console.log(event.which);
			switch (event.which) {
				case 37: // Left.
				case 38: // Up.
					index--;
					break;
				case 39: // Right.
				case 40: // Down.
					index++;
					break;
				case 36: // Home.
					index = 0;
					break;
				case 35: // End.
					index = this.$tabs.length - 1;
					break;
				default:
					return;
			}
			event.preventDefault();
			// Do we need to wrap around?
			if (index >= this.$tabs.length) {
				index = 0;
			} else if (index < 0) {
				index = this.$tabs.length - 1;
			}
			this.setActiveTabUsingKeys(this.$tabs.get(index), this.$tabContent, index);
		},
		setActiveTabUsingKeys: function (tabs, tabContent, index) {
			var $targetTab = $(tabs);
			var $targetContent = $(tabContent.children().get(index));
			if ($targetTab.hasClass('selected')) {
				return;
			}
			this.$tabs.filter('.selected').attr('aria-selected', false).closest("li").removeClass('active');
			this.$tabs.filter('.selected').attr('aria-selected', false).removeClass('selected').attr('tabindex', '-1');
			$(tabContent.children()).removeClass('in');
			$(tabContent.children()).removeClass('active');
			$targetTab.addClass('selected').attr('aria-selected', true).removeAttr('tabindex').focus();
			this.$tabs.filter('.selected').attr('aria-selected', true).closest("li").addClass('active');
			$targetContent.addClass('active');
			$targetContent.addClass('in');
		},
		setActiveTabUsingClicks: function (target) {
			var $target = $(target);
			if ($target.hasClass('selected')) {
				return;
			}
			this.$tabs.filter('.selected').attr('aria-selected', false).removeClass('selected').attr('tabindex', '-1');
			$target.addClass('selected').attr('aria-selected', true).removeAttr('tabindex').focus();
		},
	};
	$(document).ready(function () {
		TabsController.init($('.nav-tabs'), $('.tab-content'));
		$('.nav-tabs').each(function (tabIndex, tabElement) {
			$(tabElement).find('a').each(function (anchorIndex, anchorElement) {
				if (anchorIndex === 0) {
					$(anchorElement.hash).addClass('active');
					$(anchorElement.hash).addClass('in');
					$(anchorElement).closest("li").addClass('active');
					$(anchorElement).addClass('selected');
				}
			});
		});
	});
})(adnsf$);
