window.addEventListener('onWidgetLoad', function (obj) {
    const fieldData = obj.detail.fieldData;

    let dataObj = {
        api: fieldData.api,
        widgetUri: fieldData.widgetUri,
        appId: fieldData.appId,
        clientSecret: fieldData.clientSecret,
        userId: fieldData.userId,
        maxGoal: fieldData.goal,
        currency: fieldData.cur
    }

    addSubscription(dataObj); 
});

async function addSubscription(dataObj) {
    let data = await tryGetTokens(dataObj);

    if (!data) {
        return;
    }

    const widget = await getWidget(dataObj);

    if (!widget) {
        return;
    }
    
    const RAISED_AMOUNT = widget.raised_amount;

    setDonation(dataObj, dataObj.maxGoal, RAISED_AMOUNT);

    const CENTRIFUGE_API = "wss://centrifugo.donationalerts.com/connection/websocket";

    let centrifuge = new Centrifuge(CENTRIFUGE_API, {
        subscribeEndpoint: "https://www.donationalerts.com/api/v1/centrifuge/subscribe",
        subscribeHeaders: {
        "Authorization": `Bearer ${data.access_token}`
        }
    })

    centrifuge.setToken(data.socket_connection_token);
    
    centrifuge.connect();

    centrifuge.on("connect", async function() {
        console.log("connected");

        centrifuge.subscribe(`$goals:goal_${dataObj.userId}`, message => {
            let data = message.data;
            
            setDonation(dataObj, data.goal_amount, data.raised_amount);
        });
    });

    centrifuge.on("disconnect", async function() {
        console.log("disconnected");
    });
}

async function getWidget(dataObj) {
    let widgetStatus = await fetch(`${dataObj.api}/api/widget?appId=${dataObj.appId}`, {
        method: "GET"
    });

    if (!widgetStatus.ok) {
        console.error("Не удалось получить информацию о виджете");
        return;
    }

    return JSON.parse(await widgetStatus.text());
}

async function tryGetTokens(dataObj) {
    let oauthStatus = await fetch(`${dataObj.api}/api/oauth?appId=${dataObj.appId}`, {
        method: "GET"
    });


    if (!oauthStatus.ok) {
        const text = await oauthStatus.text();

        if (text.includes("App not found")) {
            return getTokensFirstTime(dataObj)
        }
        
        console.error("Не удалось получить токен подключения вебсокета");
        return;
    }

    return JSON.parse(await oauthStatus.text());
}

async function getTokensFirstTime(dataObj) {
    let connectStatus = await fetch(`${dataObj.api}/api/ping`, {
        method: "GET"
    });

    if (!connectStatus.ok) {
        console.error("Не удалось подключиться к серверу");
        return;
    }
    
    let registerStatus = await fetch(`${dataObj.api}/api/register`, {
        method: "POST",
        body: JSON.stringify({
            appId: dataObj.appId,
            goalWidgetUri: dataObj.widgetUri,
            clientSecret: dataObj.clientSecret
        }),
        headers: {
            "Content-Type": "application/json",
        },
    });

    if (!registerStatus.ok) {
        console.error("Не удалось зарегестрировать пользователя");
        return;
    }

    let uriStatus = await fetch(`${dataObj.api}/api/login/uri?appId=${dataObj.appId}`, {
        method: "GET"
    });

    if (!uriStatus.ok) {
        console.error("Не удалось получить адрес");
        return;
    }

    const openUri = await uriStatus.text();

    creatUri(openUri);

    for (let i = 10; i > 0; i++) {
        let loginStatus = await fetch(`${dataObj.api}/api/login/check`, {
            method: "GET"
        });

        if (loginStatus.ok) {
            break;
        }

        await new Promise(r => setTimeout(r, 3000));
    }

    destroyUri();

    let oauthStatus = await fetch(`${dataObj.api}/api/oauth?appId=${dataObj.appId}`, {
        method: "GET"
    });

    if (!oauthStatus.ok) {
        console.error("Не удалось получить токен подключения вебсокета");
        return;
    }

    return JSON.parse(await oauthStatus.text());
}

function creatUri(uri) {
    let container = document.getElementById("api-button");

    if (!container) {
        console.log(uri);
        return;
    }

    let link = document.createElement("a");

    link.textContent = "Ссылка на авторизацию";
    link.className = 'styled-api';
    link.id = "link";
    link.href = uri;
    
    container.appendChild(link);
}

function destroyUri() {
    let link = document.getElementById("link");

    link.remove();
}

function setDonation(dataObj, maxGoal, newGoal) {
    const addPx = "28px";   

    let bar = document.getElementsByClassName("prog")[0];
    let currency = dataObj.currency;

    document.getElementsByClassName("text")[0].textContent = `${currency}${newGoal}`;
    
    let calcGoal = (newGoal / maxGoal) * 100;
    let showGoal = calcGoal>= 100 
        ? 100
        : calcGoal; 

    bar.style.width = `calc(${showGoal}% - ${addPx})`;
}