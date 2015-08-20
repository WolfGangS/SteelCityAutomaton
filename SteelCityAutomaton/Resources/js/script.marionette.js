//284d4b14-79d0-45c5-8a36-90482da31490

var Bot = Backbone.Model.extend({
    defaults : {
        session : "",
        firstname : '',
        lastname : '',
        connected : false,
        position : {
            sim : null,
            x : 0,
            y : 0,
            z : 0
        }
    }
});

var Bots = Backbone.Collection.extend({
    model: Bot,
    url: service_url,
    parse: function(data,options){
        console.log("data :", data);
        if(!data.results) return;
        if(!data.results.length) return;
        return data.results[0].data;
        //return data;
    }
});

var bots = new Bots([]);

var BotView = Backbone.View.extend({
    model: new Bot(),
    tagName: 'tr',
    events: {
        'click .bot-login': 'loginClicked',
        'click .bot-logout': 'logoutClicked'
    },
    initialize: function(){
        this.template = _.template($('.bot-list-template').html());
    },
    render: function() {
        this.$el.html(this.template(this.model.toJSON()));
        this.$el.addClass(this.model.get('connected') ? "bot-online" : "bot-offline");
        return this;
    },
    loginClicked: function(e){
        post(bot_url(this.model.get('session')),{"commands":[{"command":"login"}]},"login_r");
    },
    logoutClicked: function(e){
        post(bot_url(this.model.get('session')),{"commands":[{"command":"logout"}]},"login_r");
    }
});

var BotsView = Backbone.View.extend({
    model: bots,
    //el: '.bots-list',
    initialize: function() {
        this.model.on('add', this.render, this);
    },
    render: function() {
        //console.log("ping");
        //console.log(this.$el.html());
        this.$el.empty();
        //console.log($('.bots-list').html());
        var self = this;
        _.each(this.model.toArray(),function(blog){
            self.$el.append((new BotView({model: blog})).render().$el);
        });
        //console.log("pong");
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
        console.log(data);
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

function new_session(frst,last,pass)
{
    var newbot = new_command("new_automaton");
    newbot.commands[0].firstname = frst;
    newbot.commands[0].lastname = last;
    newbot.commands[0].password = pass;
    //post("service", { "commands":[{"command": "new_automaton","firstname":frst,"lastname":last,"password":pass}] }, 'new_automaton_r');
    post(service_url(), newbot, 'new_automaton_r');
}

$(document).ready(function() {

    //create_chat();
     $(".api-token").val("284d4b14-79d0-45c5-8a36-90482da31490");
    botsView = new BotsView({el : '.bots-list'});
    $('.add-bot').on('click',function(){
        /*
        var bot = new Bot({
            firstname: $('.firstname-input').val(),
            lastname: $('.lastname-input').val(),
            password: $('.password-input').val()
        });
        console.log(bot.toJSON());
        $(".bot-add-form :input").val('');
        bots.add(bot);
        */
        new_session($('.firstname-input').val(),$('.lastname-input').val(),$('.password-input').val());
    });
    setInterval(function() {
        bots.fetch({data: JSON.stringify(new_command("list_automatons")), type: 'POST'});
        //post(service_url(), new_command("list_automatons"), 'bot_scrape');
    }, 5000);

    bots.fetch({data: JSON.stringify(new_command("list_automatons")), type: 'POST'});
    //post(service_url(), new_command("list_automatons"), 'bot_scrape');
});
