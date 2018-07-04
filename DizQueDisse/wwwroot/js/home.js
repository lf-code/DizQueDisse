
var AddHomeFunctionality = function () {
    //Home has no additional functionality, besides core functionality.
}

var main = function () {
    //Setting customization variables of core functionality (site_core.js):
    this.loadMoreUrl = "/home/more";
    this.changeBackground = true;

    //Add core functionality:
    AddCoreFunctionality.apply(this);

    //Add home view functionality:
    AddHomeFunctionality.apply(this);

    //Core Functionality has the entry-point.
}

$(document).ready(
    main.bind({}) //binds an empty obj as 'this', to which all functionality will be added
);


