var session = "";
var last_total_chat = -1;
var last_total_ims = -1;
var first = true;
var chat;

var im_obj = {};

var selected_attachment;
var attachment_data_time = 0;

var f_name = "WolfGang";
var l_name = "Forsythe";
/*function clear_interval(arg){
    if(typeof arg !== 'undefined')window.clearInterval(arg);
}*/

function new_session(frst,last,pass)
{
    var newbot = new_command("new_automaton");
    newbot.commands[0].firstname = frst;
    newbot.commands[0].lastname = last;
    newbot.commands[0].password = pass;
    //post("service", { "commands":[{"command": "new_automaton","firstname":frst,"lastname":last,"password":pass}] }, 'new_automaton_r');
    post(service_url(), newbot, 'new_automaton_r');
}

function login_session()
{
    $("#loginbtn").attr("disabled", "disabled");
    if(session.length > 5)post(bot_url(),{"commands":[{"command":"login"}]},"login_r");
}

function touch_obj()
{
    if($("#touch_key").val().length == 36)
    {
        touch($("#touch_key").val());
    }
}

function touch(key)
{
    if(key.length == 36)
    {
        json = new_command("touch");
        json.commands[0].uuid = key;
        post(bot_url(),json,"generic_r");
    }
}

function logout_session()
{
    $("#logoutbtn").attr("disabled", "disabled");
    if(session.length > 5)post(bot_url(),{"commands":[{"command":"logout"}]},"logout_r");
}

function new_command(cmd)
{
    return {"label":"webui","commands":[{"command":cmd,"label":"label" + cmd}]};
}

function bot_url()
{
    return $("#apitoken").val() + "/automaton/" + session;
}

function service_url()
{
    return $("#apitoken").val() + "/service";
}

function get_folder_items()
{
    inv = new_command("get_inv_items");
    if($("#inv_filter").val().length > 0)inv.commands[0].filter = $("#inv_filter").val();
    if($("#inv_folder").val().length > 0)inv.commands[0].path = $("#inv_folder").val();
    post(bot_url(),inv,"get_inv_items_r");
}

function get_folder_folders()
{
    inv = new_command("get_inv_folders");
    if($("#inv_filter").val().length > 0)inv.commands[0].filter = $("#inv_filter").val();
    if($("#inv_folder").val().length > 0)inv.commands[0].path = $("#inv_folder").val();
    post(bot_url(),inv,"get_inv_folders_r");
}

function accept_tp()
{
    var key = $("#tpos-select").val();
    var txt = $('#tpos-select :selected').text();
    json = new_command("accept_tp_offer");
    json.commands[0].user_id = key;
    post(bot_url(),json,"generic_r");
}

function decline_tp()
{
    var key = $("#tpos-select").val();
    var txt = $('#tpos-select :selected').text();
    json = new_command("decline_tp_offer");
    json.commands[0].user_id = key;
    post(bot_url(),json,"generic_r");

}

function offer_tp(key)
{
    json = new_command("offer_tp");
    json.commands[0].user_id = key;
    post(bot_url(),json,"generic_r");
}

function refresh_im(key)
{
    $("#chat_" + key + '_text').val("");
    njson = new_command("get_im_log");
    njson.commands[0].uuid = key;
    njson.commands[0].first = 0;
    post(bot_url(),njson,"get_im_log_r");
}

function key_send_chat(event,field)
{
    if(event.keyCode == 13)send_chat(field);
}

function send_chat(field)
{
    var c = $("#" + field).val();
    $("#" + field).val("");
    if(c.length > 0)
    {
        json = new_command("chat");
        json.commands[0].channel = 0;
        json.commands[0].message = c;
        post(bot_url(),json,"generic_r");
    }
}

function new_im()
{
    var c = $("#new_im_uuid").val();
    alert(c);
    send_im("new_im_msg",c);
}

function send_im(field,key)
{
    var c = $("#" + field).val();
    $("#" + field).val("");
    if(c.length > 0)
    {
        json = new_command("im");
        json.commands[0].uuid = key;
        json.commands[0].message = c;
        post(bot_url(),json,"generic_r");
    }
}

function remove_im(key)
{
    json = new_command("remove_im_session");
    json.commands[0].uuid = key;
    post(bot_url(),json,"generic_r");
    if(im_obj.hasOwnProperty(key))delete im_obj[key];
    $("#chat_" + key + "_tab").remove();
    $("#chat_" + key).remove();
    $("a[href=#local]").tab("show");
}

function touch_attachment(key)
{
    touch(key);
}

function agent_list_select()
{
    njson = new_command("get_attachment_list");
    njson.commands[0].uuid = $("agent_list").val();
    post(bot_url(),njson,"get_attachment_list_r");
}

var functions = {
    generic_r: function(data){
        console.log(data);
    },
    bot_scrape: function(data) {
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                if(json.data.length < 1){
                    $("#bot-list").html("");
                    //if(first)
                    //{
                        //first = false;
                        new_session("WGBot","Resident",prompt("Password",""));
                    //}
                    return;
                }
                var obj = json.data[0];
                var table = "<thead><tr>";
                $.each(Object.keys(obj),function(key,val){
                    table += "<th>" + val + "</th>";
                });
                table += "</tr></thead><tbody>";
                $.each(json.data,function(key,val){
                    if(val.session == session)table += '<tr class="success">';
                    else table += '<tr>';
                    table += "<td>" + val.session + "</td>";
                    table += "<td>" + val.firstname + "</td>";
                    table += "<td>" + val.lastname + "</td>";
                    table += "<td>" + val.connected + "</td>";
                    table += "</tr>";
                });
                table += "</tbody>";
                $(".table > tbody > tr").off("click");
                $("#bot-list").html(table);
                $('.table > tbody > tr').click(function() {
                    $(this).parent().children().removeClass("success");
                    $(this).addClass("success");
                    session = $(this).find("td:first").text();
                    f_name = $(this).find("td:eq(1)").text();
                    l_name = $(this).find("td:eq(2)").text();
                    last_chat = 0;
                    $(".bot-session").each(function(){
                        $(this).val(session);
                    });
                    $('a[data-toggle="tab"]').off('click');
                    $("#chat_pages").html('');
                    $("#chat_tabs").html('');
                    im_obj = {};
                    last_total_chat = -1;
                    last_total_ims = -1;
                    create_chat();
                });
                if(session.length > 5)post(bot_url(),new_command("get_status"),'bot_status');
            }
            else if(first)
            {
                first = false;
                new_session(f_name,l_name,prompt("Password",""));
            }
        }
    },
    new_automaton_r: function(data){
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            //alert("results exists");
            //console.log(json.results[0].command);

            $.each(json.results,function(key,val){
                if(val.success)
                {
                    //alert(val.data);
                    post(service_url(), new_command("list_automatons"), 'bot_scrape');
                }
            });
        }
    },
    bot_status: function(data){
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                $("#bot-status-sim").val(json.data.sim_name);
                $("#bot-status-pos").val(JSON.stringify(json.data.position));
                $("#bot-status-uuid").val(json.data.avatar_uuid);
                $("#bot-status-ims").val(json.data.total_ims);
                if(json.data.total_ims > last_total_ims)
                {
                    njson = new_command("get_im_sessions");
                    post(bot_url(),njson,"get_im_sessions_r");
                    $("#bot-status-ims").val(json.data.total_ims - (last_total_ims + 1));
                }
                else $("#bot-status-ims").val("0");
                if(json.data.total_chat > last_total_chat + 1)
                {
                    njson = new_command("get_chat_log");
                    njson.commands[0].first = last_total_chat + 1;
                    njson.commands[0].label = last_total_chat + 1;
                    post(bot_url(),njson,"get_chat_r");
                    $("#bot-status-chat").val(json.data.total_chat - (last_total_chat + 1));
                }
                else $("#bot-status-chat").val("0");
                $("#bot-status-dialogs").val(json.data.dialogs);
                var tpos = json.data.pending_tp_offers;
                $("#bot-status-tpos").val(tpos);
                if(tpos > 0)
                {
                    njson = new_command("get_teleport_offers");
                    post(bot_url(),njson,"get_teleport_offers_r");
                }
                else $("#tpos-select").html('<option value="null">None</option>');
                if(json.data.connected)
                {
                    $("#loginbtn").attr("disabled", "disabled");
                    $("#logoutbtn").removeAttr("disabled");
                    //json = new_command("get_attachment_list");
                    //json.commands[0].uuid = "677bf9a4-bba5-4cf9-a4ad-4802a0f7ef46";
                    //post(bot_url(),json,"generic_r");
                    njson = new_command("get_region_data");
                    post(bot_url(),njson,"get_region_data_r");
                }
                else
                {
                    $("#logoutbtn").attr("disabled", "disabled");
                    $("#loginbtn").removeAttr("disabled");
                }
            }
        }
    },
    get_region_data_r : function(data){
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.hasOwnProperty('data'))
            {
                var agents = json.data.agents;
                var sel = $("#agent_list").val();
                $("#agent_list").html('');
                var c = "";
                var found = false;
                $.each(agents,function(key, val){
                    if(val.user_id == sel)found = true;
                    c += '<option value="' + val.user_id + '">' + val.name + '</option>';
                });
                $("#agent_list").html(c);
                if(found)
                {
                    //alert(sel);
                    $("#agent_list").val(sel[0]);
                    //njson = new_command("get_attachment_list");
                    //njson.commands[0].uuid = sel[0];
                    //post(bot_url(),njson,"get_attachment_list_r");
                }
            }
        }
    },
    get_attachment_list_r : function(data){
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.hasOwnProperty('data'))
            {
                var sel = $("#attachment_list").val();
                $("#attachment_list").html('');
                var c = "";
                var found = false;
                $.each(json.data,function(key, val){
                    if(key == sel)found = true;
                    c += '<option value="' + key + '">' + val + '</option>';
                });
                $("#attachment_list").html(c);
                if(found)
                {
                    //alert(sel);
                    $("#attachment_list").val(sel[0]);
                    var datt = Math.floor(Date.now()/1000);
                    if(sel[0] != selected_attachment || datt > attachment_data_time)
                    {
                        attachment_data_time = datt + 60;
                        selected_attachment = sel[0];
                        $("#attachment_touch").attr("onclick","touch_attachment(\'" + sel[0] + "\')");
                        njson = new_command("get_object_data");
                        njson.commands[0].uuid = sel[0];
                        post(bot_url(),njson,"generic_r");
                    }
                }
            }
        }
    },
    get_im_sessions_r : function(data){
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.hasOwnProperty('data'))
            {
                var s = 0;
                $.each(json.data,function(key, val){
                    if(!im_obj.hasOwnProperty(key))
                    {
                        im_obj[key] = val;
                        njson = new_command("get_im_log");
                        njson.commands[0].uuid = key;
                        post(bot_url(),njson,"get_im_log_r");
                    }
                    else if(val > im_obj[key])
                    {
                        njson = new_command("get_im_log");
                        njson.commands[0].uuid = key;
                        njson.commands[0].first = im_obj[key];
                        post(bot_url(),njson,"get_im_log_r");
                    }
                    s += val;
                });
                last_total_ims = s;
            }
        }
    },
    get_im_log_r : function(data){
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.hasOwnProperty('data'))
            {
                json = json.data;
                var key = json.user_id;
                var name = json.user_name;
                var id = 0;
                var tag = "chat_" + key;
                //<span class="badge">4</span>
                var count = 0;
                var c = $("#" + tag + '_text').val();
                if(typeof c === 'undefined')c = "";
                $.each(json.messages,function(key, val){
                    if(val.id > id)id = val.id;
                    if(c.length > 0)c += "\n";
                    var from_name = name;
                    if(!val.from_other)from_name = f_name + " " + l_name;
                    else ++count;
                    if(val.message.lastIndexOf("/me",0) == 0)
                    {
                        val.message = val.message.substring(3,val.message.length);
                        c += "[" + from_name + val.message + "]";
                    }
                    else c += "[" + from_name + "] : " + val.message;
                });
                if($("#" + tag).length < 1)
                {
                    $('a[data-toggle="tab"]').off('click');
                    $("#chat_tabs").html($("#chat_tabs").html() + '<li role="presentation" class="" id="' + tag + '_tab"><a data-tag="' + tag + '" href="#' + tag + '" aria-controls="' + tag + '" role="tab" data-toggle="tab">' + name + ' <span class="badge" id="' + tag + '_badge">' + count + '</span> <span class="badge" onclick="remove_im(\'' + key + '\')">X</span></a></li>');//<span class="glyphicon glyphicon-remove-circle" aria-hidden="true"></span>
                    $("#chat_pages").html($("#chat_pages").html() + '<div role="tabpanel" class="tab-pane well" id="' + tag + '"><label >Logs : </label>'
                        + '<input type="button" value="Refresh" onclick="refresh_im(' + "'" +  key + "'" + ')"/>'
                        + '<input type="button" value="Tp" onclick="offer_tp(' + "'" +  key + "'" + ')"/>'
                        + '<textarea id="' + tag + '_text" style="width:100%;height:300px;resize:none" disabled="disabled"></textarea><br />'
                        + '<label style="width:6%">IM : </label><input type="text" id="' + tag + '_im" style="width:88%"/>'
                        + '<input type="button" value="Send" onclick="send_im(\'' + tag + '_im\',\'' + key + '\')" />'
                        + '</div>');
                    $('a[data-toggle="tab"]').on('click', function () {
                        $("#" + $(this).data("tag") + "_badge").html('');
                        //var badge = $(this).find(".badge im_counter").html('');
                        //alert(badge.html()); // newly activated tab
                    });
                }
                else
                {
                    var co = parseInt($("#" + tag + "_badge").html());
                    co += count;
                    $("#" + tag + "_badge").html(co);
                }
                $("#" + tag + '_text').val(c);
                im_obj[key] = id + 1;
            }
        }
    },
    get_teleport_offers_r : function(data){
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            //console.log(JSON.stringify(json));
            if(json.hasOwnProperty('data'))
            {
                var s;
                $.each(json.data,function(key, val){
                    s += '<option value="' + val.user_id + '">' + val.user_name + '</option>';
                });
                $("#tpos-select").html(s);
            }
            //console.log(s);

//{"label":null,"results":[{"command":"get_teleport_offers","data":[{"user_id":"677bf9a4-bba5-4cf9-a4ad-4802a0f7ef46","user_name":"WolfGang Senizen","session_id":"3b6e0c08-fdcb-35a4-1b93-6cffc21fc906","message":"Join me in Hangover Bay\nhttp://maps.secondlife.com/secondlife/Hangover%20Bay/97/54/22"}],"message":null,"success":true}]}

        }
    },
    get_inv_folders_r: function(data){
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                if(json.data.length > 0)
                {
                    var s;
                    $.each(json.data,function(key,val){
                        s += val + "\n";
                    });
                    //console.log(s);
                }
            }
        }
    },
    get_inv_items_r: function(data){
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                if(json.data.length > 0)
                {
                    var s;
                    $.each(json.data,function(key,val){
                        s += val + "\n";
                    });
                    //console.log(s);
                }
            }
            //else Console.log(json.message);
        }
    },
    get_chat_r: function(data){
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                if(json.data.length > 0)
                {
                    var nline = false;
                    if($("#chat_box").val().length > 0)nline = true;
                    var ch =  "";
                    var id = 0;
                    var count = 0;
                    $.each(json.data,function(key,val){
                        if(nline)ch += "\n";
                        else nline = true;
                        if(val.id > id)id = val.id;
                        if(val.message.lastIndexOf("/me",0) == 0)
                        {
                            val.message = val.message.substring(3,val.message.length);
                            ch += "[" + val.from_name + val.message + "]";
                        }
                        else ch += "[" + val.from_name + "] : " + val.message;
                        ++count;
                    });
                    last_total_chat = id;
                    $("#chat_box").val($("#chat_box").val() + ch);
                    var co = parseInt($("#chat_badge").html());
                    co += count;
                    $("#chat_badge").html(co);
                    //var ta = document.getElementById('chat_box');
                    //ta.scrollTop = ta.scrollHeight;
                }
            }
        }
    },
    login_r: function(data){
        //console.log(data);
        var json = JSON.parse(data);
        if(json.hasOwnProperty('results'))
        {
            json = json.results[0];
            if(json.success)
            {
                load = new_command("inv_load");
                post(bot_url(),load,"generic_r");
            }
        }
    },
    logout_r: function(data){
        //console.log(data);
    }
}

function post(url, json, func) {
    $.ajax({
        url: url,
        dataType: 'text',
        type: 'post',
        contentType: 'application/json',
        data: JSON.stringify(json),
        success: function(data, textStatus){
            if(functions[func])
            {
                functions[func](data);
            }
        }
    });
}

function copy_prompt(field)
{
    var text = $("#" + field).val();
    if(text.length > 0)window.prompt("Copy to clipboard: Ctrl+C, Enter", text);
}

function create_chat()
{
    //alert("Create Chat");
    $("#chat_tabs").html($("#chat_tabs").html() 	+ '<li role="presentation" class="active"><a href="#local" aria-controls="local" role="tab" data-toggle="tab">Local <span id="chat_badge" class="badge"></span></a></li>');
    $("#chat_pages").html($("#chat_pages").html() 	+ '<div role="tabpanel" class="tab-pane active well" id="local">'
                                                    + '<label>Logs : </label><textarea id="chat_box" disabled="disabled" style="width:100%;height:300px;resize:none;"></textarea><br />'
                                                    + '<label style="width:6%">Chat : </label><input type="text" id="new_chat" style="width:88%" onkeydown="key_send_chat(event,\'new_chat\')"/><input type="button" value="Send" onclick="send_chat(\'new_chat\')" />'
                                                    + '</div>');
}

$(document).ready(function() {

    create_chat();

    setInterval(function() {
        post(service_url(), new_command("list_automatons"), 'bot_scrape');
    }, 5000);

    post(service_url(), new_command("list_automatons"), 'bot_scrape');


    //$("#chat_tabs").html($("#chat_tabs").html() + '<li role="presentation" class=""><a href="#wgf" aria-controls="wgf" role="tab" data-toggle="tab">WG.F</a></li>');
    //$("#chat_pages").html($("#chat_pages").html() + '<div role="tabpanel" class="tab-pane well" id="wgf"><label style="width:10%">Logs : </label><textarea id="wgf-chat" disabled="disabled" style="width:90%"></textarea><br /><label style="width:10%">wgf : </label><input type="text" id="wgf-new_chat" style="width:90%"/></div>');
    //post("service", { "commands":[{"command": "new_automaton","firstname":"wolfgang","lastname":"forsythe","password":prompt("Password","")}] }, 'new_automaton_r');
});
