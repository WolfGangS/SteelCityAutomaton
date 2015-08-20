//284d4b14-79d0-45c5-8a36-90482da31490

var active_session = null;

var Position = function(){
    this.X = 0;
    this.Y = 0;
    this.Z = 0;
}
/*
var Message = function(){
    this.message = null;
    this.timestamp = 0;
    this.from_other = false;
    this.id = -1;
}*/

var IMSession = Backbone.Model.extend({
    defaults:{
        user_id : '00000000-0000-0000-0000-000000000000',
        user_name : null,
        messages : []
    }
});

var Bot = Backbone.Model.extend({
    defaults : {
        session : "",
        firstname : '',
        lastname : '',
        connected : false,
        group_id : '00000000-0000-0000-0000-000000000000',
        avatar_uuid : '00000000-0000-0000-0000-000000000000',
        total_dialogs:0,
        total_chat:0,
        pending_tp_offers:0,
        permission_requests:0,
        total_ims:1,
        balance:0,
        tpoffers : [],
        sim_name: null,
        position : new Position()
    }/*,
    constructor : function()
    {
        this.group_id = '00000000-0000-0000-0000-000000000000';
        this.avatar_uuid = '00000000-0000-0000-0000-000000000000';
        this.total_dialogs = 0;
        this.total_chat = 0;
        this.pending_tp_offers = 0;
        this.permission_requests = 0;
        this.total_ims = 1;
        this.balance = 0;
        this.tpoffers = [];
        this.sim_name = "";
        this.position = new Position();
        Backbone.Model.apply(this, arguments);
    }*/
});

var Bots = Backbone.Collection.extend({
    model: Bot,
    url: service_url,
    parse: function(data,options){
        //console.log("options : ", options);
        if(!data.results) return;
        if(!data.results.length) return;
        data = data.results[0].data;
        if($.isArray(data))
        {
            for(i = 0; i < data.length; ++i)
            {
                var d = bots.find(function(model){ return model.get('session') === data[i].session;});
                if(d)
                {
                    data[i] = $.extend(d.attributes,data[i]);
                }
            }
            if(data.length > 0 && active_session)
            {
                post(bot_url(active_session),new_command("get_status"),'bot_status');
            }
        }
        return data;
    }
});

var bots = new Bots([]);

var ChatTabView = Backbone.View.extend({
    initialize: function(){
        this.template = _.template($('.bot-data-template').html());
    },
    render: function(mod) {
        this.$el.html(this.template(mod.toJSON()));
        return this;
    }
})

var BotDataView = Backbone.View.extend({
    tagName: 'div',
    className: 'form-horizontal',
    initialize: function(){
        this.template = _.template($('.bot-data-template').html());
        //this.model.on('change', this.render, this);
    },
    render: function(mod) {
        if(!mod) this.$el.empty();
        else this.$el.html(this.template(mod.toJSON()));
        return this;
    }
});

var BotView = Backbone.View.extend({
    model: new Bot(),
    tagName: 'tr',
    //className: 'bot-row',
    events: {
        'click .bot-login': 'loginClicked',
        'click .bot-logout': 'logoutClicked',
        'click .bot-delete': 'deleteClicked',
        'click .bot-row' : 'rowClicked'
    },
    initialize: function(){
        this.template = _.template($('.bot-list-template').html());
    },
    render: function() {
        this.$el.html(this.template(this.model.toJSON()));
        this.$el.addClass(this.model.get('connected') ? "bot-online" : "bot-offline");
        this.$el.addClass(this.model.get('session') == active_session ? "success" : "");
        return this;
    },
    loginClicked: function(e){
        post(bot_url(this.model.get('session')),{"commands":[{"command":"login"}]},"login_r");
    },
    logoutClicked: function(e){
        post(bot_url(this.model.get('session')),{"commands":[{"command":"logout"}]},"login_r");
    },
    deleteClicked: function(e){
        post(service_url(),{"commands":[{"command":"destroy_automaton","session":this.model.get('session')}]},"destroy_automaton_r");
    },
    rowClicked: function(e){
        if(active_session === this.model.get('session'))
        {
            active_session = null;
            $('.bot-data').hide();
        }
        else
        {
            active_session = this.model.get('session');
            botDataView.render(this.model);
            post(bot_url(active_session),new_command("get_status"),'bot_status');
            $('.bot-data').show();
        }
        botsView.render();
    }
});
//bots.filter(function(model){ return model.get('connected') === true; })
var BotsView = Backbone.View.extend({
    model: bots,
    initialize: function() {
        this.model.on('add', this.render, this);
    },
    render: function() {
        this.$el.empty();
        var self = this;
        _.each(this.model.toArray(),function(blog){
            self.$el.append((new BotView({model: blog})).render().$el);
        });
        return this;
    }
});

function post(url, json, func) {
    $.ajax({
        url: url,
        dataType: 'text',
        type: 'post',
        contentType: 'application/json',
        data: JSON.stringify(json),
        success: function(data, textStatus)
        {
            if(functions[func])
            {
                functions[func](data);
            }
            else functions["generic_r"](data);
        }
    });
}

var functions = {
    generic_r: function(data){
        console.log(data);
    },
    new_automaton_r:function(data){
        fetch_bots();
    },
    destroy_automaton_r:function(data){
        fetch_bots();
    },
    bot_status: function(data){
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                botDataView.render(bots.find(function(model){ return model.get('session') === active_session;}).set(json.data));
                if(json.data.total_ims > 0){
                    post(bot_url(active_session),new_command("get_im_sessions"),"get_im_sessions_r");
                }
            }
        }
    },
    get_im_sessions_r: function(data){
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                $.each(json.data,function(key,val){
                    njson = new_command("get_im_log");
                    njson.commands[0].uuid = key;
                    njson.commands[0].first = 0;
                    post(bot_url(active_session),njson,"get_im_log_r");
                });
            }
        }
    },
    get_im_log_r: function(data)
    {
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                //console.log(data);
            }
        }
    }
}

function new_command(cmd)
{
    return {"label":"webui","commands":[{"command":cmd,"label":"label" + cmd}]};
}

function bot_url(session)
{
    return $(".api-token").val() + "/automaton/" + session;
}

function service_url()
{
    return $(".api-token").val() + "/service";
}

var botsView;
var botDataView;
function new_session(frst,last,pass)
{
    var newbot = new_command("new_automaton");
    newbot.commands[0].firstname = frst;
    newbot.commands[0].lastname = last;
    newbot.commands[0].password = pass;
    post(service_url(), newbot, 'new_automaton_r');
}

function fetch_bots()
{
    bots.fetch({data: JSON.stringify(new_command("list_automatons")), type: 'POST'});
}

$(document).ready(function() {

    //create_chat();
    $(".api-token").val("5827d9b4-2db9-4b5d-9c6b-6de248f466cc");
//  5827d9b4-2db9-4b5d-9c6b-6de248f466cc
//  00000000-0000-0000-0000-000000000000
    botsView = new BotsView({el : '.bots-list'});
    $('.bot-data').show();
    botDataView = new BotDataView({el : '.bot-data-disp'});
    $('.add-bot').on('click',function(){
        new_session($('.firstname-input').val(),$('.lastname-input').val(),$('.password-input').val());
        $(".bot-add-form :input").val('');
    });
    setInterval(function() {
        fetch_bots();
    }, 5000);

    fetch_bots();
});
