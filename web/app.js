function app() {
    let lessThan60Seconds = [];
    let lessThan180Seconds = [];
    let lessThan300Seconds = [];
    let lessThan600Seconds = [];
    let longLastingDeployments = [];

    function reqListener(event) {
        let response = JSON.parse(event.target.response);
        let deployments = response.deployments;

        for (let i = 0; i < deployments.length; i++) {
            let deployment = deployments[i];
            determineBucket(deployment);
        }

        renderBuckets();
    }

    function determineBucket(deployment) {
        let deploymentTime = deployment.durations[0];

        if (deploymentTime < 60) {
            lessThan60Seconds.push(deployment);
            return;
        }

        if (deploymentTime < 180) {
            lessThan180Seconds.push(deployment);
            return;
        }

        if (deploymentTime < 300) {
            lessThan300Seconds.push(deployment);
            return;
        }

        if (deploymentTime < 600) {
            lessThan600Seconds.push(deployment);
            return;
        }

        if (deploymentTime >= 600) {
            longLastingDeployments.push(deployment);
            return;
        }
    }

    function renderBuckets() {
        renderBucket('lessThan60Services', lessThan60Seconds);
        renderBucket('lessThan180Services', lessThan180Seconds);
        renderBucket('lessThan300Services', lessThan300Seconds);
        renderBucket('lessThan600Services', lessThan600Seconds);
        renderBucket('longLastingServices', longLastingDeployments);
    }

    function renderBucket(elementId, bucket) {
        var htmlElement = document.getElementById(elementId);
        for (let i = 0; i < bucket.length; i++) {
            let service = bucket[i];
            let row = document.createElement('tr');
            let minutes = Math.floor(service.durations[0] / 60);
            let seconds = service.durations[0] - minutes * 60;
            let html = `<td>${service.serviceName}</td><td>${minutes}m ${seconds}s</td><td></td>`;
            row.innerHTML = html;
            htmlElement.appendChild(row);
        }
    }

    var request = new XMLHttpRequest();
    request.addEventListener("load", reqListener.bind(this));
    request.open("GET", "http://tocoffeeazure.cloud/result");
    //request.open("GET", "https://localhost:5001/result");
    request.send();
}

var app = new app();