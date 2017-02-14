// VERSION = '1.0.1'
if (!jQuery) { throw new Error("xfront.js requires jQuery") }
// requires bootstrapValidator.js
// requires moment.js
// bootstrap-datetimepicker.js

//------app-----------------------------------------------------------------------------------------

// querier options: {pageIndex, done(data)}
// ('getPage')
+function ($) {
    "use strict";

    // Class
    var Querier = function (element, options) {
        this.$element = $(element);
        this.options = $.extend({}, Querier.DEFAULTS, options);
        delete this.options.index;
    };

    Querier.VERSION = '1.0.1';

    Querier.DEFAULTS = {
        "done": undefined
    };

    Querier.prototype.query = function (pageIndex) {
        var instance = this;
        var $querier = this.$element;
        var $done = this.options.done;
        var obj = $querier.serializeObject();
        obj.pageIndex = pageIndex;
        $.getJSON('/Data', obj, function (data, textStatus, jqXHR) {
            $querier.find('[data-plugin=renderer]').renderer(data);

            // filterer
            $querier.find('[data-plugin=filterer]').each(function (index, element) {
                var $filterer = $(element);
                if ($filterer.is(':button')) {
                    $filterer.click(function (event) {
                        instance.query(0);
                        event.preventDefault();
                    });
                }
            });

            // sorter
            $querier.find('[data-plugin=sorter]').each(function (index, element) {
                var $sorter = $(element);
                if ($sorter.is('select')) {
                    sorter.change(function (event) {
                        instance.query(0);
                        event.preventDefault();
                    });
                }
            });

            // tableHeadSorter
            $querier.find('[data-plugin=tableHeadSorter]').tableHeadSorter({
                "change": function () {
                    instance.query(0);
                }
            });

        }).done(function (data, textStatus, jqXHR) {
            $.getJSON('/Data/Count', obj, function (data, textStatus, jqXHR) {
                var pageIndexChange = function (pageIndex) {
                    instance.query(pageIndex);
                };

                var itemCount = data.Count;
                var pageIndex = obj.pageIndex;

                var pageSize;
                var pageSizer = $querier.find('[data-plugin=pageSizer]');
                if (pageSizer.is('select')) {
                    pageSize = pageSizer.val();
                    pageSizer.change(function (event) {
                        instance.query(0);
                        event.preventDefault();
                    });
                }

                var pageCount = Math.ceil(itemCount / pageSize);

                instance.page = {};
                instance.page.itemCount = itemCount;
                instance.page.pageIndex = pageIndex;
                instance.page.pageSize = pageSize;
                instance.page.pageCount = pageCount;

                $querier.find('[data-plugin=pagination]').pagination({
                    "pageIndex": pageIndex,
                    "pageCount": pageCount,
                    "change": function (index) {
                        pageIndexChange(index);
                    }
                });
                $querier.find('[data-plugin=paginationGo]').paginationGo({
                    "pageIndex": pageIndex,
                    "pageCount": pageCount,
                    "change": function (index) {
                        pageIndexChange(index);
                    }
                });
                $querier.find('[data-plugin=paginationInfo]').paginationInfo({
                    "pageSize": pageSize,
                    "pageIndex": pageIndex,
                    "itemCount": itemCount
                });
            });

            if ($done != null) {
                $done(data);
            }
        });
    };

    Querier.prototype.getPage = function () {
        return this.page;
    };

    // Plugin 
    function Plugin(options) {
        if (typeof options == 'string') {
            var $this = $(this);
            var data = $this.data('xd.querier');
            if (options == 'getPage') {
                if (!data) return null;
                return data.getPage();
            }
        }
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.querier');
            var opts = typeof options == 'object' && options;
            if (!data) $this.data('xd.querier', (data = new Querier(this, opts)));
            var pageIndex = (options.pageIndex == null) ? 0 : options.pageIndex;
            data.query(pageIndex);
        })
    }

    var old = $.fn.querier;

    $.fn.querier = Plugin;
    $.fn.querier.Constructor = Querier;

    // No conflict
    $.fn.querier.noConflict = function () {
        $.fn.querier = old;
        return this;
    };

}(jQuery);

// modalEditor options: {"creator":{title,submitText,done(data)}, "editor":{title,submitText,done(data)}} // requires bootstrapValidator
// ('create',{name,data}),('edit',data)
+function ($) {
    "use strict";

    // Class
    var ModalEditor = function (element, options) {
        this.$element = $(element);
        this.options = $.extend({}, ModalEditor.DEFAULTS);
        if (options) {
            this.options.creator = $.extend({}, this.options.creator, typeof options.creator == 'object' && options.creator);
            this.options.editor = $.extend({}, this.options.editor, typeof options.editor == 'object' && options.editor);
        }

        this.$form = this.$element.find('form');
        this.$form.bootstrapValidator({ excluded: ':disabled' });

        var action = this.$form.attr('action');
        if (action != null) {
            this.options.creator.action = action;
            this.options.editor.action = action;
        }

        var creator_action = this.$form.attr('data-creator-action');
        if (creator_action != null) {
            this.options.creator.action = creator_action;
        }

        var editor_action = this.$form.attr('data-editor-action');
        if (editor_action != null) {
            this.options.creator.action = editor_action;
        }

        this.$submit = this.$element.find('.modal-footer :button[data-dismiss!=modal]');

        var instance = this;
        this.$element.on('hidden.bs.modal', function (event) {
            instance.$submit.off('click');
            instance.$form.bootstrapValidator('resetForm', true);
            instance.$form[0].reset();
            instance.$form.find('input[type=hidden]').val('');
        });
    };

    ModalEditor.VERSION = '1.0.1';

    ModalEditor.DEFAULTS = {
        creator: {
            title: "Create",
            submitText: "Create",
            done: undefined
        },
        editor: {
            title: "Edit",
            submitText: "Update",
            done: undefined
        }
    };

    ModalEditor.prototype.create = function (master) {
        var $element = this.$element;
        var $form = this.$form;
        var action = this.options.creator.action;
        var name = $form.attr('data-creator-name');
        var $submit = this.$submit;

        var done = this.options.creator.done;

        $element.find('.modal-title').html(this.options.creator.title);
        $submit.html(this.options.creator.submitText);

        $submit.on('click', function (event) {
            var bootstrapValidator = $form.data('bootstrapValidator');
            bootstrapValidator.validate();
            var isValid = bootstrapValidator.isValid();
            if (!isValid) return;

            var obj = $form.serializeObject();
            if (master != null) {
                obj[master.name] = master.data;
            }

            var url = (action == null) ? '/Data' : action;
            url = (name == null) ? url : url + '?name=' + name;
            $.ajax({
                "async": true,
                "url": url,
                "type": "POST",
                "contentType": "application/json",
                "data": JSON.stringify(obj),
                "dataType": "json",
                "success": function (data, textStatus, jqXHR) {
                    $form.find(':password').val('');
                    if (data.Error == null) {
                        $element.modal('hide');
                        if (done != null) {
                            done(data);
                        }
                    }
                    else {
                        var msg = $.xfont._getErrorMessage(data.Error)
                        alert(msg);
                    }
                },
            });
        });

        $.getJSON('/Data?name=' + name, {}, function (data, textStatus, jqXHR) {
            var obj = $.isArray(data) ? data[0] : data;
            $form.deserializeObject(obj);
            $element.modal();
        });
    };

    ModalEditor.prototype.edit = function (data) {
        var $element = this.$element;
        var $form = this.$form;
        var action = this.options.editor.action;
        var name = $form.attr('data-editor-name');
        var $submit = this.$submit;

        var done = this.options.editor.done;

        $element.find('.modal-title').html(this.options.editor.title);
        $submit.html(this.options.editor.submitText);

        $submit.on('click', function (event) {
            var bootstrapValidator = $form.data('bootstrapValidator');
            bootstrapValidator.validate();
            var isValid = bootstrapValidator.isValid();
            if (!isValid) return;

            var obj = $form.serializeObject();
            var url = (action == null) ? '/Data' : action;
            url = (name == null) ? url : url + '?name=' + name;
            $.ajax({
                "async": true,
                "url": url,
                "type": "PUT",
                "contentType": "application/json",
                "data": JSON.stringify(obj),
                "dataType": "json",
                "success": function (data, textStatus, jqXHR) {
                    $form.find(':password').val('');
                    if (data.Error == null) {
                        $element.modal('hide');
                        if (done != null) {
                            done(data);
                        }
                    }
                    else {
                        var msg = $.xfont._getErrorMessage(data.Error)
                        alert(msg);
                    }
                },
            });
        });

        $.getJSON('/Data?name=' + name, data, function (data, textStatus, jqXHR) {
            var obj = $.isArray(data) ? data[0] : data;
            $form.deserializeObject(obj);
            $element.modal();
        });
    };

    // Plugin 
    function Plugin(options, arg) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.modalEditor');
            var opts = typeof options == 'object' && options;
            if (!data) $this.data('xd.modalEditor', (data = new ModalEditor(this, opts)));
            if (options == 'create') {
                data.create(arg);
            }
            else if (options == 'edit') {
                data.edit(arg);
            }
        })
    }

    var old = $.fn.modalEditor;

    $.fn.modalEditor = Plugin;
    $.fn.modalEditor.Constructor = ModalEditor;

    // No conflict
    $.fn.modalEditor.noConflict = function () {
        $.fn.modalEditor = old;
        return this;
    };

}(jQuery);

// $.deleteData(name, data, done())
(function ($) {
    "use strict";

    var DeleteData = function (name, data, done) {
        var url = (name == null) ? '/Data/Batch' : '/Data/Batch?name=' + name;
        $.ajax({
            "async": true,
            "url": url,
            "type": "DELETE",
            "contentType": "application/json",
            "data": JSON.stringify(data),
            "dataType": "json",
            "success": function (data, textStatus, jqXHR) {
                if (data.Error == null) {
                    if (done != null) {
                        done();
                    }
                }
                else {
                    var msg = $.xfont._getErrorMessage(data.Error)
                    alert(msg);
                }
            },
        });
    };

    DeleteData.VERSION = '1.0.1';

    $.deleteData = DeleteData;

})(jQuery);

//------site----------------------------------------------------------------------------------------

// queryPage options: {done(data, page)}
+function ($) {
    "use strict";

    // Class
    var QueryPage = function (element, options) {
        this.$element = $(element);
        this.options = $.extend({}, QueryPage.DEFAULTS, options);
    };

    QueryPage.VERSION = '1.0.1';

    QueryPage.DEFAULTS = {
        "done": undefined
    };

    QueryPage.prototype.initialize = function () {
        var $querier = this.$element;
        var $done = this.options.done;

        var searchObj = $.parseObject();
        if (searchObj.pageIndex == null) searchObj.pageIndex = 0;
        $querier.deserializeObject(searchObj);

        var obj = $querier.serializeObject();
        obj.pageIndex = searchObj.pageIndex;

        $.getJSON('/Data', obj, function (data, textStatus, jqXHR) {
            $querier.find('[data-plugin=renderer]').renderer(data);
        }).done(function (data, textStatus, jqXHR) {

            // filterer
            $querier.find('[data-plugin=filterer]').each(function (index, element) {
                var filterer = $(element);
                if (filterer.is(':button')) {
                    var data_name = filterer.attr('data-name');
                    filterer.click(function (event) {
                        var elementNames = filterer.attr('data-value-elements').split(',');
                        for (var i = 0; i < elementNames.length; i++) {
                            var elementName = $.trim(elementNames[i]);
                            var element = $('[name=' + elementName + ']');
                            if (element.length > 0) {
                                searchObj[elementName] = element.val();
                            }
                        }
                        if (data_name == null) {
                            delete searchObj.name
                        }
                        else {
                            searchObj.name = data_name;
                        }
                        delete searchObj.pageIndex;
                        window.location.href = $.toEncodedUrl(searchObj);
                        event.preventDefault();
                    });
                }
            });

            // sorter
            $querier.find('[data-plugin=sorter]').each(function (index, element) {
                var sorter = $(element);
                if (sorter.is('select')) {
                    var name = sorter.attr('name');
                    sorter.change(function (event) {
                        var sort = sorter.val();
                        searchObj[name] = sort;
                        delete searchObj.pageIndex;
                        window.location.href = $.toEncodedUrl(searchObj);
                        event.preventDefault();
                    });
                }
            });

            // tableHeadSorter
            $querier.find('[data-plugin=tableHeadSorter]').tableHeadSorter({
                "change": function (value) {
                    searchObj = $.extend(searchObj, value);
                    delete searchObj.pageIndex;
                    window.location.href = $.toEncodedUrl(searchObj);
                }
            });

            var r_data = data;
            var r_page;
            $.getJSON('/Data/Count', obj, function (data, textStatus, jqXHR) {
                var pageIndexChange = function (pageIndex) {
                    if (pageIndex == 0) {
                        delete searchObj.pageIndex;
                    }
                    else {
                        searchObj.pageIndex = pageIndex;
                    }
                    window.location.href = $.toEncodedUrl(searchObj);
                };

                var pageSizeChange = function (name, pageSize) {
                    searchObj[name] = pageSize;
                    delete searchObj.pageIndex;
                    window.location.href = $.toEncodedUrl(searchObj);
                };

                var itemCount = data.Count;

                var pageIndex = searchObj.pageIndex;

                var pageSize;
                var pageSizer = $querier.find('[data-plugin=pageSizer]');
                if (pageSizer.is('select')) {
                    var name = pageSizer.attr('name');
                    pageSize = pageSizer.val();
                    pageSizer.change(function (event) {
                        var size = pageSizer.val();
                        pageSizeChange(name, size);
                        event.preventDefault();
                    });
                }

                var pageCount = Math.ceil(itemCount / pageSize);

                r_page = {};
                r_page.itemCount = itemCount;
                r_page.pageIndex = pageIndex;
                r_page.pageSize = pageSize;
                r_page.pageCount = pageCount;

                $querier.find('[data-plugin=pagination]').pagination({
                    "pageIndex": pageIndex,
                    "pageCount": pageCount,
                    "change": function (index) {
                        pageIndexChange(index);
                    }
                });
                $querier.find('[data-plugin=paginationGo]').paginationGo({
                    "pageIndex": pageIndex,
                    "pageCount": pageCount,
                    "change": function (index) {
                        pageIndexChange(index);
                    }
                });
                $querier.find('[data-plugin=paginationInfo]').paginationInfo({
                    "pageSize": pageSize,
                    "pageIndex": pageIndex,
                    "itemCount": itemCount
                });

            }).done(function (data, textStatus, jqXHR) {
                if ($done != null) {
                    $done(r_data, r_page);
                }
            });
        });
    };

    // Plugin 
    function Plugin(options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.queryPage');
            var opts = typeof options == 'object' && options;
            if (!data) $this.data('xd.queryPage', (data = new QueryPage(this, opts)));
            data.initialize();
        })
    }

    var old = $.fn.queryPage;

    $.fn.queryPage = Plugin;
    $.fn.queryPage.Constructor = QueryPage;

    // No conflict
    $.fn.queryPage.noConflict = function () {
        $.fn.queryPage = old;
        return this;
    };

}(jQuery);

// createForm options: {submitText} // requires bootstrapValidator
+function ($) {
    "use strict";

    // Class
    var CreateForm = function (form, options) {
        this.$form = $(form);
        this.options = $.extend({}, CreateForm.DEFAULTS, options);
    };

    CreateForm.VERSION = '1.0.1';

    CreateForm.DEFAULTS = {
        submitText: 'Create'
    };

    CreateForm.prototype.initialize = function () {
        var $form = this.$form;
        var submitText = this.options.submitText;
        var $submit = $form.find(':submit');
        var redirect = $submit.attr("data-redirect-url") + window.location.search;
        $submit.val(submitText).attr("disabled", true);

        var data_name = $form.attr('data-name');
        var getUrl = (data_name == null) ? '/Data' : '/Data?name=' + data_name;
        $.getJSON(getUrl, {}, function (data, textStatus, jqXHR) {
            var obj = $.isArray(data) ? data[0] : data;
            $form.deserializeObject(obj);
        }).done(function () {
            $form.bootstrapValidator({
                submitHandler: function (validator, form, submitButton) {
                    var obj = $(form).serializeObject();
                    $.ajax({
                        "async": true,
                        "url": "/Data",
                        "type": "POST",
                        "contentType": "application/json",
                        "data": JSON.stringify(obj),
                        "dataType": "json",
                        "success": function (data, textStatus, jqXHR) {
                            $form.find(':password').val('');
                            if (data.Error == null) {
                                window.location.href = redirect;
                            }
                            else {
                                $form.find('[data-plugin=backendErrorRenderer]').backendErrorRenderer(data.Error);
                            }
                        },
                    });
                },
            });

            $form.find(':submit').attr("disabled", false);
        });
    };

    // Plugin 
    function Plugin(options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.createForm');
            var opts = typeof options == 'object' && options;
            if (!data) {
                $this.data('xd.createForm', (data = new CreateForm(this, opts)));
                data.initialize();
            }
        })
    }

    var old = $.fn.createForm;

    $.fn.createForm = Plugin;
    $.fn.createForm.Constructor = CreateForm;

    // No conflict
    $.fn.createForm.noConflict = function () {
        $.fn.createForm = old;
        return this;
    };

}(jQuery);

// editForm options: {submitText} // requires bootstrapValidator
+function ($) {
    "use strict";

    // Class
    var EditForm = function (form, options) {
        this.$form = $(form);
        this.options = $.extend({}, EditForm.DEFAULTS, options);
    };

    EditForm.VERSION = '1.0.1';

    EditForm.DEFAULTS = {
        submitText: 'Edit'
    };

    EditForm.prototype.initialize = function () {
        var $form = this.$form;
        var submitText = this.options.submitText;
        var $submit = $form.find(':submit');
        var redirect = $submit.attr("data-redirect-url") + window.location.search;
        $submit.val(submitText).attr("disabled", true);

        var data_name = $form.attr('data-name');
        var getUrl = (data_name == null) ? '/Data' : '/Data?name=' + data_name;
        $.getJSON(getUrl, {}, function (data, textStatus, jqXHR) {
            var obj = $.isArray(data) ? data[0] : data;
            $form.deserializeObject(obj);
        }).done(function (data, textStatus, jqXHR) {
            $form.bootstrapValidator({
                submitHandler: function (validator, form, submitButton) {
                    var obj = $form.serializeObject();
                    $.ajax({
                        "async": true,
                        "url": "/Data",
                        "type": "PUT",
                        "contentType": "application/json",
                        "data": JSON.stringify(obj),
                        "dataType": "json",
                        "success": function (data, textStatus, jqXHR) {
                            $form.find(':password').val('');
                            if (data.Error == null) {
                                window.location.href = redirect;
                            }
                            else {
                                $form.find('[data-plugin=backendErrorRenderer]').backendErrorRenderer(data.Error);
                            }
                        },
                    });
                },
            });

            $form.find(':submit').attr("disabled", false);
        });
    };

    // Plugin 
    function Plugin(options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.editForm');
            var opts = typeof options == 'object' && options;
            if (!data) {
                $this.data('xd.editForm', (data = new EditForm(this, opts)));
                data.initialize();
            }
        })
    }

    var old = $.fn.editForm;

    $.fn.editForm = Plugin;
    $.fn.editForm.Constructor = EditForm;

    // No conflict
    $.fn.editForm.noConflict = function () {
        $.fn.editForm = old;
        return this;
    };

}(jQuery);

// deleteForm
+function ($) {
    "use strict";

    // Class
    var DeleteForm = function (element) {
        this.$element = $(element);
    };

    DeleteForm.VERSION = '1.0.1';

    DeleteForm.prototype.initialize = function () {
        var $el = this.$element;
        var $submit = $el.find(':submit');
        var redirect = $submit.attr("data-redirect-url") + window.location.search;

        $submit.click(function (event) {
            event.preventDefault();

            $.ajax({
                "async": true,
                "url": "/Data",
                "type": "DELETE",
                "contentType": "application/json",
                "data": {},
                "dataType": "json",
                "success": function (data, textStatus, jqXHR) {
                    $el.find(':password').val('');
                    if (data.Error == null) {
                        window.location.href = redirect;
                    }
                    else {
                        $el.find('[data-plugin=backendErrorRenderer]').backendErrorRenderer(data.Error);
                    }
                },
            });

        });
    };

    // Plugin 
    function Plugin(options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.deleteForm');
            var opts = typeof options == 'object' && options;
            if (!data) {
                $this.data('xd.deleteForm', (data = new DeleteForm(this, opts)));
                data.initialize();
            }
        })
    }

    var old = $.fn.deleteForm;

    $.fn.deleteForm = Plugin;
    $.fn.deleteForm.Constructor = DeleteForm;

    // No conflict
    $.fn.deleteForm.noConflict = function () {
        $.fn.deleteForm = old;
        return this;
    };

}(jQuery);

// $('select[data-name]').initializer()
+function ($) {
    "use strict";

    // Class
    var Initializer = function (element) {
        this.$element = $(element);
    };

    Initializer.VERSION = '1.0.1';

    Initializer.prototype.initialize = function () {
        var $el = this.$element;
        var emptyHtml = $el.attr('data-empty-html');
        var name = $el.attr('data-name');
        $.getJSON('/Data?name=' + name, {}, function (data) {
            $el.renderer(data);
            if (emptyHtml != null) {
                var html = '<option selected="selected" value="">' + emptyHtml + '</option>';
                html += $el.html();
                $el.html(html);
            }
        });
    };

    // Plugin 
    function Plugin(options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.initializer');
            var opts = typeof options == 'object' && options;
            if (!data) {
                $this.data('xd.initializer', (data = new Initializer(this, opts)));

                $.ajaxSettings.async = false;
                data.initialize();
                $.ajaxSettings.async = false;
            }
        })
    }

    var old = $.fn.initializer;

    $.fn.initializer = Plugin;
    $.fn.initializer.Constructor = Initializer;

    // No conflict
    $.fn.initializer.noConflict = function () {
        $.fn.initializer = old;
        return this;
    };

}(jQuery);

//------plugins-------------------------------------------------------------------------------------

// serializeObject() // requires moment
+function ($) {
    "use strict";

    var getTimezoneString = function () {
        var offset = new Date().getTimezoneOffset();
        var abs_offset = Math.abs(offset);
        var hours = Math.floor(abs_offset / 60);
        var mins = abs_offset % 60;

        var suffix = (hours < 10) ? '0' + hours : hours.toString();
        suffix += (mins < 10) ? '0' + mins : mins.toString();
        suffix = (offset < 0) ? '+' + suffix : '-' + suffix;
        return suffix;
    }

    var getElementValue = function (element) {
        var value = undefined;

        var plugin = $(element).attr('data-plugin');
        if (plugin == 'datetimepicker') {
            var m = $(element).parent().data("DateTimePicker").date();
            if (m == null) return '';
            var number = m.valueOf();
            var suffix = getTimezoneString();
            value = '/Date(' + number + suffix + ')/';
            return value;
        }

        if ($(element).is('select')) {
            value = $(element).val();
        }

        if ($(element).is('textarea')) {
            value = $(element).val();
            value = value.replace(/\n/gm, '\\r\\n');
        }

        if ($(element).is('input')) {
            var type = $(element).attr('type');
            switch (type) {
                case 'radio':
                    var checked = $(element).prop("checked");
                    if (checked) {
                        value = $(element).val();
                    }
                    else {
                        return null;
                    }
                    break;
                case 'checkbox':
                    var el_value = $(element).attr('value');
                    var checked = $(element).prop("checked");
                    if (el_value == null) {
                        return checked;
                    }
                    else {                       
                        if (checked) {
                            value = el_value;
                        }
                        else {
                            return null;
                        }
                    }
                    break;
                case 'file':
                case 'image':
                case 'button':
                case 'submit':
                case 'reset':
                    break;
                default:
                    value = $(element).val();
            }
        }

        if (value == '') return value;

        var dataType = $(element).attr('data-data-type');
        if (dataType == 'Date') {
            var format = $(element).attr('data-date-format');
            var m = (format == null) ? moment(value) : moment(value, format);
            var number = m.valueOf();
            var suffix = getTimezoneString();
            value = '/Date(' + number + suffix + ')/';
        }

        if (dataType == 'Number') {
            value = +value;
        }

        if (dataType == 'Boolean') {
            if (value.toString() == 'true') value = true;
            if (value.toString() == 'false') value = false;
        }

        return value;
    };

    var SerializeObject = function (element) {
        var obj = {};
        $(element).find(':input[name]').each(function (index, el) {
            var name = $(el).attr('name');
            var value = getElementValue(el);
            if (value != null) obj[name] = value;
        });
        return obj;
    };

    SerializeObject.VERSION = '1.0.1';

    // Plugin
    var old = $.fn.serializeObject;

    $.fn.serializeObject = function () {
        var result = {};
        this.each(function () {
            var obj = SerializeObject(this);
            result = $.extend({}, result, obj);
        })
        return result;
    };

    // No conflict
    $.fn.serializeObject.noConflict = function () {
        $.fn.serializeObject = old;
        return this;
    };

}(jQuery);

// deserializeObject(obj) // requires moment
+function ($) {
    "use strict";

    var setElementValue = function (element, value) {
        var plugin = $(element).attr('data-plugin');
        if (plugin == 'datetimepicker') {
            if (value == null && value == '') {
                $(element).parent().data("DateTimePicker").date(null);
            }
            else {
                var m = moment(value);
                $(element).parent().data("DateTimePicker").date(m);
            }
            return
        }

        var val = value;

        var format = $(element).attr('data-date-format');
        if (format == null) {
            if (isNaN(val) && val.indexOf('/Date(') == 0) {
                var m = moment(val);
                if (m.isValid) {
                    val = m.format('YYYY-MM-DD');
                    if (!moment(val).isSame(m)) {
                        val = m.format('YYYY-MM-DD HH:mm:ss')
                    }
                }
            }
        }
        else {
            var m = moment(val);
            val = m.format(format);
        }

        if ($(element).is('select,textarea')) {
            $(element).val(val);
        }

        if ($(element).is('input')) {
            var type = $(element).attr('type');
            switch (type) {
                case 'radio':
                    var el_val = $(element).val();
                    $(element).prop("checked", el_val == val);
                    break;
                case 'checkbox':
                    var el_value = $(element).attr('value');
                    if (el_value == null) {
                        if (val.toString() == 'true') {
                            $(element).prop("checked", true);
                        }
                        else {
                            $(element).prop("checked", false);
                        }
                    }
                    else {
                        $(element).prop("checked", el_value == val);
                    }
                    break;
                default:
                    $(element).val(val);
                    break;
            }
        }
    }

    var DeserializeObject = function (element, obj) {
        if ($.isArray(obj)) {
            for (var i = 0; i < obj.length; i++) {
                var single = obj[i];
                for (var name in single) {
                    var val = single[name];
                    $(element).find(':checkbox[name=' + name + ']').filter('[value=' + val + ']').prop("checked", true);
                }
            }
            return;
        }

        $(element).find(':input[name]').each(function (index, el) {
            var name = $(el).attr('name');
            var value = obj[name];
            if (value != undefined) setElementValue(el, value);
        });
    };

    DeserializeObject.VERSION = '1.0.1';

    // Plugin
    var old = $.fn.deserializeObject;

    $.fn.deserializeObject = function (obj) {
        return this.each(function () {
            DeserializeObject(this, obj);
        })
    };

    // No conflict
    $.fn.deserializeObject.noConflict = function () {
        $.fn.deserializeObject = old;
        return this;
    };

}(jQuery);

// renderer(data)
+function ($) {
    "use strict";

    // Class
    var Renderer = function (container) {
        this.$container = $(container);
    };

    Renderer.VERSION = '1.0.1';

    Renderer.prototype.render = function (data) {
        $.xfont._render(this.$container, data);
    };

    // Plugin 
    var old = $.fn.renderer;

    $.fn.renderer = function (data) {
        return this.each(function () {
            var $this = $(this);
            var a_data = $this.data('xd.renderer');
            if (!a_data) $this.data('xd.renderer', (a_data = new Renderer(this)));
            a_data.render(data);
        })
    };

    $.fn.renderer.Constructor = Renderer;

    // No conflict
    $.fn.renderer.noConflict = function () {
        $.fn.renderer = old;
        return this;
    };

}(jQuery);

// backendErrorRenderer(error)
+function ($) {
    "use strict";

    // Class
    var BackendErrorRenderer = function (element) {
        this.$element = $(element);
    };

    BackendErrorRenderer.VERSION = '1.0.1';

    BackendErrorRenderer.prototype.render = function (error) {
        var html = '';
        if (error.ExceptionType == 'XData.Data.Element.Validation.ElementValidationException') {
            var errorMessage = error.ValidationErrors.ValidationError.ErrorMessage;
            if (errorMessage == null) {
                for (var index in error.ValidationErrors.ValidationError) {
                    var err = error.ValidationErrors.ValidationError[index];
                    html += '<li>' + err.ErrorMessage + '</li>';
                }
            }
            else {
                html = '<li>' + errorMessage + '</li>';
            }
        }
        else {
            html = '<li>' + error.ExceptionMessage + '</li>';
        }

        var $el = this.$element;
        if (!$el.is('ul,ol')) {
            html = '<ul>' + html + '</ul>';
        }

        $el.html(html);
    };

    // Plugin 
    var old = $.fn.backendErrorRenderer;

    $.fn.backendErrorRenderer = function (error) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.backendErrorRenderer');
            if (!data) $this.data('xd.backendErrorRenderer', (data = new BackendErrorRenderer(this)));
            data.render(error);
        })
    };

    $.fn.backendErrorRenderer.Constructor = BackendErrorRenderer;

    // No conflict
    $.fn.backendErrorRenderer.noConflict = function () {
        $.fn.backendErrorRenderer = old;
        return this;
    };

}(jQuery);

// pagination options: {prev,next,pageIndex,pageCount,change(pageIndex)}
+function ($) {
    "use strict";

    // Class
    var Pagination = function (element, options) {
        this.$element = $(element);

        var data = {};
        var prev = this.$element.attr('data-prev');
        if (prev != null) data.prev = prev;
        var next = this.$element.attr('data-next');
        if (next != null) data.next = next;

        this.options = $.extend({}, Pagination.DEFAULTS, data, options);
    };

    Pagination.VERSION = '1.0.1';

    Pagination.DEFAULTS = {
        "prev": "< Prev",
        "next": "Next >",
        "pageIndex": 0
    };

    Pagination.prototype.setOptions = function (options) {
        this.options = $.extend({}, this.options, options);

        var $el = this.$element;
        var prev = this.options.prev;
        var next = this.options.next;
        var pageIndex = this.options.pageIndex;
        var pageCount = this.options.pageCount;
        var change = this.options.change;

        var ul_html = '';
        if (pageCount == null) {
            ul_html = '';
        }
        else if (pageCount < 11) {
            for (var i = 0; i < pageCount; i++) {
                if (i == pageIndex) {
                    ul_html += '<li class="active"><a href="javascript:">' + (+i + 1) + '</a></li>';
                }
                else {
                    ul_html += '<li><a href="javascript:">' + (+i + 1) + '</a></li>';
                }
            }
        }
        else if (pageIndex < 5) {
            for (var i = 0; i < 6; i++) {
                if (i == pageIndex) {
                    ul_html += '<li class="active"><a href="javascript:">' + (i + 1) + '</a></li>';
                }
                else {

                    ul_html += '<li><a href="javascript:">' + (i + 1) + '</a></li>';
                }
            }
            ul_html += '<li><span>...</span></li>';
            ul_html += '<li><a href="javascript:">' + pageCount + '</a></li>';
            ul_html += '<li><a href="javascript:">' + next + '</a></li>';
        }
        else if (pageCount - pageIndex < 6) {
            ul_html += '<li><a href="javascript:">' + prev + '</a></li>';
            ul_html += '<li><a href="javascript:">1</a></li>';
            ul_html += '<li><span>...</span></li>';
            for (var i = pageCount - 6; i < pageCount ; i++) {
                if (i == pageIndex) {
                    ul_html += '<li class="active"><a href="javascript:">' + (+i + 1) + '</a></li>';
                }
                else {

                    ul_html += '<li><a href="javascript:">' + (+i + 1) + '</a></li>';
                }
            }
        }
        else {
            ul_html += '<li><a href="javascript:">' + prev + '</a></li>';
            ul_html += '<li><a href="javascript:">1</a></li>';
            ul_html += '<li><span>...</span></li>';

            ul_html += '<li><a href="javascript:">' + (+pageIndex - 1) + '</a></li>';
            ul_html += '<li><a href="javascript:">' + pageIndex + '</a></li>';
            ul_html += '<li class="active"><a href="javascript:">' + (+pageIndex + 1) + '</a></li>';
            ul_html += '<li><a href="javascript:">' + (+pageIndex + 2) + '</a></li>';
            ul_html += '<li><a href="javascript:">' + (+pageIndex + 3) + '</a></li>';

            ul_html += '<li><span>...</span></li>';
            ul_html += '<li><a href="javascript:">' + pageCount + '</a></li>';
            ul_html += '<li><a href="javascript:">' + next + '</a></li>';
        }

        $el.html(ul_html);

        //
        $el.find('>li:not(.active) a').click(function (event) {
            var index = $(this).text();
            if (index.indexOf(prev) > -1) {
                index = pageIndex;
            }
            else if (index.indexOf(next) > -1) {
                index = pageIndex + 2;
            }
            if (change != null) {
                change(index - 1);
            }
            event.preventDefault();
        });
    };

    // Plugin 
    var old = $.fn.pagination;

    $.fn.pagination = function (options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.pagination');
            var opts = typeof options == 'object' && options;
            if (!data) $this.data('xd.pagination', (data = new Pagination(this, opts)));
            data.setOptions(opts);
        })
    };

    $.fn.pagination.Constructor = Pagination;

    // No conflict
    $.fn.pagination.noConflict = function () {
        $.fn.pagination = old;
        return this;
    };

}(jQuery);

// paginationGo options: {pageIndex,pageCount,change(pageIndex)}
+function ($) {
    "use strict";

    // Class
    var PaginationGo = function (element, options) {
        this.$element = $(element);
        var valueElement = this.$element.attr('data-value-element');
        this.$valueElement = $(valueElement);
        if (this.$valueElement.length == 0) {
            this.$valueElement = $('#' + valueElement);
        }
        this.options = $.extend({}, PaginationGo.DEFAULTS, options);

        //
        var instance = this;
        var $el = this.$element;
        $el.click(function (event) {
            var $ve = instance.$valueElement;
            if ($ve.length == 0) {
                $ve = $('#' + instance.$valueElement);
            }
            var pageCount = instance.options.pageCount;
            var index = $ve.val();
            var isValid = !isNaN(index) && index > 0;
            if (pageCount != null) {
                isValid = isValid && index <= pageCount;
            }
            if (isValid) {
                var change = instance.options.change;
                if (change != null) {
                    change(index - 1);
                }
            }
            else {
                $ve.focus().select();
            }
            event.preventDefault();
        });
    };

    PaginationGo.VERSION = '1.0.1';

    PaginationGo.DEFAULTS = {
        "pageIndex": 0
    };

    PaginationGo.prototype.setOptions = function (options) {
        this.options = $.extend({}, this.options, options);
        var pageIndex = this.options.pageIndex;
        var pageCount = this.options.pageCount;

        var $el = this.$element;
        var $ve = this.$valueElement;
        if ($ve.attr('type') == 'number') {
            $ve.attr('min', 1);
            if (pageCount != null) {
                $ve.attr('max', pageCount);
            }
        }

        if (pageCount == null) {
            $ve.val('');
        }
        else {
            var page_index = pageIndex + 2;
            if (page_index > pageCount) page_index = pageCount;
            $ve.val(page_index);
        }
    };

    // Plugin 
    var old = $.fn.paginationGo;

    $.fn.paginationGo = function (options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.paginationGo');
            var opts = typeof options == 'object' && options;
            if (!data) $this.data('xd.paginationGo', (data = new PaginationGo(this, opts)));
            data.setOptions(opts);
        })
    };

    $.fn.paginationGo.Constructor = PaginationGo;

    // No conflict
    $.fn.paginationGo.noConflict = function () {
        $.fn.paginationGo = old;
        return this;
    };

}(jQuery);

// paginationInfo options: {pageIndex,itemCount,pageSize}
+function ($) {
    "use strict";

    // Class
    var PaginationInfo = function (element, options) {
        this.$element = $(element);
        this.options = $.extend({}, PaginationInfo.DEFAULTS, options);
    };

    PaginationInfo.VERSION = '1.0.1';

    PaginationInfo.DEFAULTS = {
        "pageIndex": 0
    };

    PaginationInfo.prototype.setOptions = function (options) {
        this.options = $.extend({}, this.options, options);

        var $el = this.$element;

        var html = $el.attr('data-html');
        if (html == null) {
            html = $el.html();
            $el.attr('data-html', html);
            $el.html('');
        }

        var pageIndex = this.options.pageIndex;
        var itemCount = this.options.itemCount;
        var pageSize = this.options.pageSize;
        var pageCount = Math.ceil(itemCount / pageSize);

        if (itemCount == null) return;

        var var_statements = 'var pageIndex=' + pageIndex + ';';
        var_statements += ' var itemCount=' + itemCount + ';';
        var_statements += ' var pageSize=' + pageSize + ';';
        var_statements += ' var pageCount=' + pageCount + ';';

        // {{...}}
        html = html.replace(/\{{2}.*?\}{2}/g, function (word) {
            var exp = word.slice(2, -2);
            exp = var_statements + ' ' + exp;
            return eval(exp);
        });
        $el.html(html);
    };

    // Plugin 
    var old = $.fn.paginationInfo;

    $.fn.paginationInfo = function (options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.paginationInfo');
            var opts = typeof options == 'object' && options;
            if (!data) $this.data('xd.paginationInfo', (data = new PaginationInfo(this, opts)));
            data.setOptions(opts);
        })
    };

    $.fn.paginationInfo.Constructor = PaginationInfo;

    // No conflict
    $.fn.paginationInfo.noConflict = function () {
        $.fn.paginationInfo = old;
        return this;
    };

}(jQuery);

// tableHeadSorter options: {value,change(value)} // value: {"header":2,"updown":1}
+function ($) {
    "use strict";

    // Class
    var TableHeadSorter = function (element, options) {
        this.$element = $(element);
        var headerElement = this.$element.attr('data-header-element');
        this.$headerElement = $(headerElement);
        if (this.$headerElement.length == 0) {
            this.$headerElement = $('#' + headerElement);
        }
        var updownElement = this.$element.attr('data-updown-element');
        this.$updownElement = $(updownElement);
        if (this.$updownElement.length == 0) {
            this.$updownElement = $('#' + updownElement);
        }
        this.options = $.extend({}, TableHeadSorter.DEFAULTS, options);

        //
        var instance = this;
        var $th = this.$element.find('th.sort-both, th.sort-asc, th.sort-desc');
        $th.click(function (event) {
            var $he = instance.$headerElement;
            var $ud = instance.$updownElement;
            var change = instance.options.change;

            if ($(this).hasClass('sort-asc')) {
                $(this).removeClass('sort-asc');
                $(this).addClass('sort-desc');
                var header = $(this).attr('data-header');
                $he.val(header);
                $ud.val(1);
            }
            else if ($(this).hasClass('sort-desc')) {
                $(this).removeClass('sort-desc');
                $(this).addClass('sort-asc');
                var header = $(this).attr('data-header');
                $he.val(header);
                $ud.val(0);
            }
            else if ($(this).hasClass('sort-both')) {
                $th.removeClass('sort-asc').removeClass('sort-desc').addClass('sort-both');
                $(this).removeClass('sort-both');
                $(this).addClass('sort-asc');
                var header = $(this).attr('data-header');
                $he.val(header);
                $ud.val(0);
            }
            else {
                change = null;
            }

            if (change != null) {
                var value = {};
                value[$he.attr('name')] = $he.val();
                value[$ud.attr('name')] = $ud.val();
                change(value);
            }

            event.preventDefault();
        });
    };

    TableHeadSorter.VERSION = '1.0.1';

    TableHeadSorter.DEFAULTS = {
    };

    TableHeadSorter.prototype.setOptions = function (options) {
        this.options = $.extend({}, this.options, options);
        var $he = this.$headerElement;
        var $ud = this.$updownElement;
        var value = options.value;
        if (value != null) {
            $he.val(value[$he.attr('name')]);
            var ud = value[$ud.attr('name')];
            if (ud = 'asc') ud = 0;
            if (ud = 'desc') ud = 1;
            $ud.val(ud);
        }

        var $th = this.$element.find('th.sort-both, th.sort-asc, th.sort-desc');
        $th.removeClass('sort-asc').removeClass('sort-desc').addClass('sort-both');
        var header = $he.val();
        var updown = ($ud.val() == 0) ? 'asc' : 'desc';
        $th.filter('[data-header=' + header + ']').removeClass('sort-both').addClass('sort-' + updown);
    };

    // Plugin 
    function Plugin(options) {
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.tableHeadSorter');
            var opts = typeof options == 'object' && options;
            if (!data) $this.data('xd.tableHeadSorter', (data = new TableHeadSorter(this, opts)));
            data.setOptions(opts);
        })
    }

    var old = $.fn.tableHeadSorter;

    $.fn.tableHeadSorter = Plugin;
    $.fn.tableHeadSorter.Constructor = TableHeadSorter;

    // No conflict
    $.fn.tableHeadSorter.noConflict = function () {
        $.fn.tableHeadSorter = old;
        return this;
    };

}(jQuery);

//------functions-----------------------------------------------------------------------------------

// $.toEncodedUrl(obj, path)
(function ($) {
    "use strict";

    var ToEncodedUrl = function (obj, path) {
        var search = '';
        $.each(obj, function (index, element) {
            search += index + '=' + element + '&';
        });
        if (search != '') {
            search = search.slice(0, search.length - 1);
            search = '?' + search;
        }

        if (path == null) {
            path = window.location.pathname;
        }
        return encodeURI(path + search);
    };

    ToEncodedUrl.VERSION = '1.0.1';

    $.toEncodedUrl = ToEncodedUrl;

})(jQuery);

// $.parseObject(encodedUrl)
(function ($) {
    "use strict";

    var ParseObject = function (encodedUrl) {
        var url = (encodedUrl == null) ? window.location.href : encodedUrl;
        url = decodeURI(url);

        var obj = {};
        var index = url.indexOf('?');
        if (index == -1) return obj;
        var search = url.slice(index + 1);
        var array = search.split('&');
        for (var i = 0; i < array.length; i++) {
            var pair = array[i].split('=');
            var key = pair[0];
            var value = pair[1];
            obj[key] = value;
        }
        return obj;
    };

    ParseObject.VERSION = '1.0.1';

    $.parseObject = ParseObject;

})(jQuery);

// $.xfont._render($container, data) // requires moment
(function ($) {
    "use strict";

    var Render = function ($container, data) {

        var getHtml = function (template, data) {
            //{{...}}
            var var_statements = '';
            for (var name in data) {
                var value = data[name];
                if (value == null) value = '';

                if (isNaN(value)) {
                    var m = moment(value);
                    if (m.isValid()) {
                        value = m.format('YYYY-MM-DD');
                        if (!moment(value).isSame(m)) {
                            value = m.format('YYYY-MM-DD HH:mm:ss')
                        }
                    }
                }

                var_statements += ' var ' + name + '="' + value + '";';
            }
            var result = template.replace(/\{{2}.*?\}{2}/g, function (word) {
                var exp = word.slice(2, -2);
                exp = var_statements + ' ' + exp;
                return eval(exp);
            });
            return result;
        }

        var html = $container.attr('data-html');
        if (html == null) {
            html = $container.html();
            $container.attr('data-html', html);
            $container.html('');
        }

        if (data == null) return;

        var aggr = '';
        $.each(data, function (index, item) {
            aggr += getHtml(html, item);
        });
        $container.html(aggr);

        $container.find('[data-date-format]').each(function (index, element) {
            var dateFormat = $(element).attr('data-date-format');
            if ($(element).is(':input')) {
                var value = $(element).val();
                if (value != '') {
                    var m = moment(value);
                    value = m.format(dateFormat);
                    $(element).val(value);
                }
            }
            else {
                var text = $(element).text();
                if (text != '') {
                    var m = moment(text);
                    text = m.format(dateFormat);
                    $(element).text(text);
                }
            }
        });

        $container.removeClass('hidden').removeClass('invisible');
    };

    Render.VERSION = '1.0.1';

    $.xfont = $.extend({}, $.xfont,
    {
        _render: Render
    });

})(jQuery);

// $.xfont._getErrorMessage(error)
(function ($) {
    "use strict";

    var GetErrorMessage = function (error) {
        var message = '';
        if (error.ExceptionType == 'XData.Data.Element.Validation.ElementValidationException') {
            var errorMessage = error.ValidationErrors.ValidationError.ErrorMessage;
            if (errorMessage == null) {
                for (var index in error.ValidationErrors.ValidationError) {
                    var err = error.ValidationErrors.ValidationError[index];
                    message += err.ErrorMessage + '\n';
                }
            }
            else {
                message = errorMessage + '\n';
            }
        }
        else {
            message = error.ExceptionMessage + '\n';
        }
        return message;
    };

    GetErrorMessage.VERSION = '1.0.1';

    $.xfont = $.extend({}, $.xfont,
    {
        _getErrorMessage: GetErrorMessage
    });

})(jQuery);

//------dataBox-------------------------------------------------------------------------------------

// dataBox options: {data, selectedIndex, selectedClass, selectedChanged(selectedIndex), checkedIndexes, checkedClass, checkedChanged(checkedIndexes)}
// ('getSelectedIndex'), ('getCheckedIndexes'), ('getData', index), ('getSelectedData'), ('getCheckedData')
// ('checkAll'), ('uncheckAll')
// ('append', {data(one/array)}), ('insert', {index, data(one/array)}), ('delete', index(es)), ('update', {index, data(one)}/[{index, data(one)}, {index, data(one)}...])
+function ($) {
    "use strict";

    // Class
    var DataBox = function (container) {
        this.$container = $(container);
        this.options = $.extend({}, DataBox.DEFAULTS);

        this.hasCheckboxHeader = this.$container.children().find('[data-header] input[type=checkbox]').length > 0;
        this.$checkboxAll = undefined;
        var instance = this;

        // tbody
        if (this.$container.is('tbody')) {
            if (this.$container.children(':eq(0)').children(':eq(0)').hasClass('header')) {
                var th = this.$container.parent().children('thead').children(':eq(0)').children(':eq(0)');
                if (th.find('input[type=checkbox]').length == 1) {
                    this.$checkboxAll = th.find('input[type=checkbox]');
                    this.$checkboxAll.click(function (event) {
                        if ($(this).prop('checked')) {
                            instance.checkAll();
                        }
                        else {
                            instance.uncheckAll();
                        }
                    })
                }
            }
        };
    };

    DataBox.VERSION = '1.0.1';

    DataBox.DEFAULTS = {
        selectedClass: "info",
        selectedChanged: undefined,
        checkedClass: "active",
        checkedChanged: undefined
    };

    DataBox.prototype.onSelectedChanged = function (selectedIndex) {
        var selectedChanged = this.options.selectedChanged;
        if (selectedChanged != null) {
            selectedChanged(selectedIndex);
        }
    };

    DataBox.prototype.onCheckedChanged = function (checkedIndexes) {
        if (!this.hasCheckboxHeader) return;

        var $checkboxAll = this.$checkboxAll;
        if ($checkboxAll != null) {
            var len = this.$container.children().length;
            if (len == 0) {
                $checkboxAll.prop('checked', false);
            }
            else {
                $checkboxAll.prop('checked', checkedIndexes.length == len)
            }
        }

        var checkedChanged = this.options.checkedChanged;
        if (checkedChanged != null) {
            checkedChanged(checkedIndexes);
        }
    };

    DataBox.prototype.setting = function (opts, ignoreOnChanged) {
        var $container = this.$container;
        this.options = $.extend(this.options, opts);
        delete this.options.data;
        delete this.options.selectedIndex;
        delete this.options.checkedIndexes;

        var selectedClass = this.options.selectedClass;
        var checkedClass = this.options.checkedClass;

        var setSelected = function (selectedIndex) {
            $container.children().removeClass(checkedClass).removeClass(selectedClass);
            $container.children().find('[data-header] input[type=checkbox]').prop('checked', false);

            if (selectedIndex > -1) {
                $container.children(':eq(' + selectedIndex + ')').addClass(selectedClass);
                $container.children(':eq(' + selectedIndex + ')').find('[data-header] input[type=checkbox]').prop('checked', true);
            }
        };

        var setChecked = function (checkedIndexes) {
            var checkedStr = checkedIndexes.toString();

            $container.children().find('[data-header] input[type=checkbox]').each(function (index, element) {
                if ($(element).prop('checked')) {
                    var idx = checkedStr.indexOf(index);
                    if (idx == -1) {
                        $(element).prop('checked', false);
                        $(element).parent().parent().removeClass(checkedClass).removeClass(selectedClass);
                    }
                }
                else {
                    var idx = checkedStr.indexOf(index);
                    if (idx != -1) {
                        $(element).prop('checked', true);
                        $(element).parent().parent().addClass(checkedClass);
                    }
                }
            });
        };

        var data = opts.data;
        var selectedIndex = opts.selectedIndex;
        var checkedIndexes = opts.checkedIndexes;
        if (data == null) {
            if (selectedIndex == null && checkedIndexes == null) return;

            if (selectedIndex != null && selectedIndex > -1) {
                if (checkedIndexes == null) {
                    checkedIndexes = [selectedIndex];
                }
                else {
                    if (checkedIndexes.toString().indexOf(selectedIndex) == -1)
                        checkedIndexes.push(selectedIndex);
                }
            }

            var old_selectedIndex = this.getSelectedIndex();
            var old_checkedIndexes = this.getCheckedIndexes();

            if (selectedIndex != null && selectedIndex != old_selectedIndex) {
                setSelected(selectedIndex);
                this.onSelectedChanged(selectedIndex);
            }

            if (checkedIndexes != null) {
                checkedIndexes.sort(function (a, b) {
                    return a - b;
                });
                if (checkedIndexes.toString() != old_checkedIndexes.toString()) {
                    setChecked(checkedIndexes);
                    this.onCheckedChanged(checkedIndexes);
                }
            }
        }
        else {
            // (data != null)

            var instance = this;

            $.xfont._render($container, data);

            // header-checkbox
            $container.children().find('[data-header] input[type=checkbox]').click(function (event) {
                var old_sel = instance.getSelectedIndex();

                if ($(this).prop('checked')) {
                    $(this).parent().parent().addClass(checkedClass);
                }
                else {
                    $(this).parent().parent().removeClass(checkedClass).removeClass(selectedClass);
                }

                var new_sel = instance.getSelectedIndex();
                if (new_sel != old_sel) {
                    instance.onSelectedChanged(new_sel);
                }

                instance.onCheckedChanged(instance.getCheckedIndexes());
            });

            // items
            $container.children().each(function (index, element) {
                $(element).attr('data-data', JSON.stringify(data[index]));

                // item selected
                $(element).children('[data-header!=""]').mousedown(function (event) {
                    var old_sel = instance.getSelectedIndex();
                    var old_chk = instance.getCheckedIndexes();

                    $container.children().removeClass(checkedClass).removeClass(selectedClass);
                    $(this).parent().addClass(selectedClass);
                    $container.children().find('[data-header] input[type=checkbox]').prop('checked', false);
                    $(this).parent().find('[data-header] input[type=checkbox]').prop('checked', true);

                    var new_sel = instance.getSelectedIndex();
                    if (new_sel != old_sel) {
                        instance.onSelectedChanged(new_sel);
                    }

                    var new_chk = instance.getCheckedIndexes();
                    if (new_chk.toString() != old_chk.toString()) {
                        instance.onCheckedChanged(new_chk);
                    }
                });
            });

            if (selectedIndex == null) selectedIndex = -1;
            if (checkedIndexes == null) checkedIndexes = [];

            if (selectedIndex > -1) {
                if (checkedIndexes.toString().indexOf(selectedIndex) == -1)
                    checkedIndexes.push(selectedIndex);
            }

            checkedIndexes.sort(function (a, b) {
                return a - b;
            });

            setSelected(selectedIndex);
            setChecked(checkedIndexes);

            if (ignoreOnChanged) return;

            this.onSelectedChanged(selectedIndex);
            this.onCheckedChanged(checkedIndexes);
        }
    };

    DataBox.prototype.getSelectedIndex = function () {
        var selectedClass = this.options.selectedClass;
        var selected = this.$container.children('.' + selectedClass);
        return selected.index();
    };

    DataBox.prototype.getCheckedIndexes = function () {
        var indexes = [];
        var checkedClass = this.options.checkedClass;
        this.$container.children().each(function (index, element) {
            var checked = $(element).find('[data-header] input[type=checkbox]').prop('checked');
            if (checked) {
                indexes.push($(element).index());
            }
        });
        return indexes;
    };

    DataBox.prototype.getData = function () {
        var data = [];
        this.$container.children().each(function (index, element) {
            data.push(JSON.parse($(this).attr('data-data')));
        });
        return data;
    };

    DataBox.prototype.checkAll = function () {
        var $container = this.$container;
        var checkedClass = this.options.checkedClass;
        var checkedChanged = this.options.checkedChanged;

        var checkedAll = true;
        $container.children().find('[data-header] input[type=checkbox]').each(function (index, element) {
            if (!$(element).prop('checked')) {
                checkedAll = false;
                return false;
            }
        });

        if (!checkedAll) {
            $container.children().find('[data-header] input[type=checkbox]').each(function (index, element) {
                if (!$(element).prop('checked')) {
                    $(element).prop('checked', true);
                    $(element).parent().parent().addClass(checkedClass);
                }
            });

            if (checkedChanged != null) {
                var len = $container.children().length;
                var indexes = [];
                for (var i = 0; i < len; i++) {
                    indexes.push(i);
                }
                checkedChanged(indexes);
            }
        }
    };

    DataBox.prototype.uncheckAll = function () {
        var $container = this.$container;
        var selectedClass = this.options.selectedClass;
        var checkedClass = this.options.checkedClass;
        var selectedChanged = this.options.selectedChanged;
        var checkedChanged = this.options.checkedChanged;

        var hasChecked = false;
        var selectedIndex = this.getSelectedIndex();
        if (selectedIndex > -1) {
            hasChecked = true;
        }
        else {
            $container.children().find('[data-header] input[type=checkbox]').each(function (index, element) {
                if ($(element).prop('checked')) {
                    hasChecked = true;
                    return false;
                }
            });
        }

        if (hasChecked) {
            $container.children().find('[data-header] input[type=checkbox]').prop('checked', false);
            $container.children().removeClass(checkedClass).removeClass(selectedClass);
            if (selectedIndex > -1) {
                if (selectedChanged != null) {
                    selectedChanged(-1);
                }
            }
            if (checkedChanged != null) {
                checkedChanged([]);
            }
        }
    };

    DataBox.prototype.append = function (data) {
        var selectedIndex = this.getSelectedIndex();
        var checkedIndexes = this.getCheckedIndexes();
        var newData = this.getData().concat(data);

        this.setting({
            "data": newData,
            "selectedIndex": selectedIndex,
            "checkedIndexes": checkedIndexes
        },
        true);
    };

    DataBox.prototype.insert = function (arg) {
        var index = arg.index;
        var data = [];
        if ($.isArray(arg.data)) {
            data = arg.data.concat();
        }
        else {
            data.push(arg.data);
        }

        var selectedIndex = this.getSelectedIndex();
        var checkedIndexes = this.getCheckedIndexes();
        var checkedStr = checkedIndexes.toString();
        var thisData = this.getData();

        var newData = [];
        var newSelected = -1;
        var newChecked = [];
        for (var i = 0; i < index; i++) {
            newData.push(thisData[i]);
            if (i == selectedIndex) newSelected = i;
            if (checkedStr.indexOf(i) != -1) newChecked.push(i);
        }

        newData = newData.concat(data);

        var dataLen = data.length;
        for (var i = index; i < thisData.length; i++) {
            newData.push(thisData[i]);
            if (i == selectedIndex) newSelected = i + dataLen;
            if (checkedStr.indexOf(i) != -1) newChecked.push(i + dataLen);
        }

        this.setting(
        {
            "data": newData,
            "selectedIndex": newSelected,
            "checkedIndexes": newChecked
        },
        true);
    };

    DataBox.prototype.delete = function (index) {
        var indexes = [];
        if ($.isArray(index)) {
            indexes = index.concat();
            indexes.sort(function (a, b) { return a - b; });
        }
        else {
            indexes.push(index);
        }
        var delStr = indexes.toString();

        var selectedIndex = this.getSelectedIndex();
        var checkedIndexes = this.getCheckedIndexes();
        var checkedStr = checkedIndexes.toString();
        var data = this.getData();

        var newData = [];
        var newSelected = -1;
        var newChecked = [];
        for (var i = 0; i < data.length; i++) {
            if (delStr.indexOf(i) == -1) {
                newData.push(data[i]);
                var newLen = newData.length - 1;
                if (i == selectedIndex) newSelected = newLen
                if (checkedStr.indexOf(i) != -1) newChecked.push(newLen);
            }
        }

        this.setting(
            {
                "data": newData,
                "selectedIndex": newSelected,
                "checkedIndexes": newChecked
            },
            true);

        if (newSelected != selectedIndex) {
            this.onSelectedChanged(newSelected);
        }
        if (newChecked.toString() != checkedStr) {
            this.onCheckedChanged(newChecked);
        }
    };

    DataBox.prototype.update = function (arg) {
        var items = [];
        if ($.isArray(arg)) {
            items = arg.concat();
        }
        else {
            items.push(arg);
        }

        var selectedIndex = this.getSelectedIndex();
        var checkedIndexes = this.getCheckedIndexes();
        var checkedStr = checkedIndexes.toString();
        var newData = this.getData();

        var selectedIsChanged = false;
        var checkedIsChanged = false;
        for (var i = 0; i < items.length; i++) {
            var index = items[i].index;
            var data = items[i].data;

            newData[index] = data;

            if (index == selectedIndex) {
                selectedIsChanged = true;
            }
            if (checkedStr.indexOf(index) != -1) {
                checkedIsChanged = true;
            }
        }

        this.setting(
        {
            "data": newData,
            "selectedIndex": selectedIndex,
            "checkedIndexes": checkedIndexes
        },
        true);
    };

    DataBox.prototype.getSelectedData = function () {
        var selectedIndex = this.getSelectedIndex();
        if (selectedIndex == -1) return null;
        return this.getData()[selectedIndex];
    };

    DataBox.prototype.getCheckedData = function () {
        var result = [];
        var data = this.getData();
        var indexes = this.getCheckedIndexes();
        for (var i = 0; i < indexes.length; i++) {
            result.push(data[indexes[i]]);
        }
        return result;
    };

    // Plugin 
    function Plugin(options, arg) {
        if (typeof options == 'string') {
            var $this = $(this);
            var data = $this.data('xd.dataBox');
            if (options == 'getSelectedIndex') {
                if (!data) return -1;
                return data.getSelectedIndex();
            }
            else if (options == 'getCheckedIndexes') {
                if (!data) return [];
                return data.getCheckedIndexes();
            }
            else if (options == 'getSelectedData') {
                if (!data) return null;
                return data.getSelectedData();
            }
            else if (options == 'getCheckedData') {
                if (!data) return [];
                return data.getCheckedData();
            }
            else if (options == 'getData') {
                if (arg == null) {
                    if (!data) return [];
                    return data.getData();
                }
                else {
                    if (!data) return null;
                    return data.getData()[arg];
                }
            }
        }
        return this.each(function () {
            var $this = $(this);
            var data = $this.data('xd.dataBox');
            if (!data) $this.data('xd.dataBox', (data = new DataBox(this)));
            if (typeof options == 'object') {
                data.setting(options);
            }
            if (typeof options == 'string') {
                if (options == 'checkAll') {
                    data.checkAll();
                }
                else if (options == 'uncheckAll') {
                    data.uncheckAll();
                }
                else if (options == 'append') {
                    data.append(arg);
                }
                else if (options == 'insert') {
                    data.insert(arg);
                }
                else if (options == 'delete') {
                    data.delete(arg);
                }
                else if (options == 'update') {
                    data.update(arg);
                }
            }
        })
    }

    var old = $.fn.dataBox;

    $.fn.dataBox = Plugin;
    $.fn.dataBox.Constructor = DataBox;

    // No conflict
    $.fn.dataBox.noConflict = function () {
        $.fn.dataBox = old;
        return this;
    };

}(jQuery);
