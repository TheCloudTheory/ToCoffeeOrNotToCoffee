function app() {
    let lessThan60Seconds = [];
    let lessThan180Seconds = [];
    let lessThan300Seconds = [];
    let lessThan600Seconds = [];
    let longLastingDeployments = [];

    function reqListener(event) {
        let response = JSON.parse(event.target.response);
        let deployments = response;

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
            let html = '';
            let stability = `<div class="ui label">${calculateStability(service.durations)}</div>`;

            if (minutes === 0) {
                html = `<td>${service.serviceName}</td><td>${seconds}s</td><td>${stability}</td>`;
            } else {
                html = `<td>${service.serviceName}</td><td>${minutes}m ${seconds}s</td><td>${stability}</td>`;
            }

            row.innerHTML = html;
            htmlElement.appendChild(row);
        }

        function calculateStability(durations) {
            let q25 = quantile(durations, .25);
            let q50 = quantile(durations, .50);
            let q75 = quantile(durations, .75);
            let q99 = quantile(durations, .99);

            let ratio = q50 / q99 * 100;
            if(ratio > 90) {
                return 'Good!';
            }

            if(ratio > 50) {
                return 'Average';
            }

            return 'Bad :(';
        }

        function asc(arr) {
            return arr.sort((a, b) => a - b);
        }

        function sum(arr) {
            return arr.reduce((a, b) => a + b, 0);
        }

        function mean(arr) {
            return sum(arr) / arr.length;
        }

        function std(arr) {
            const mu = mean(arr);
            const diffArr = arr.map(a => (a - mu) ** 2);
            return Math.sqrt(sum(diffArr) / (arr.length - 1));
        }

        function quantile(arr, q) {
            const sorted = asc(arr);
            const pos = ((sorted.length) - 1) * q;
            const base = Math.floor(pos);
            const rest = pos - base;
            if ((sorted[base + 1] !== undefined)) {
                return sorted[base] + rest * (sorted[base + 1] - sorted[base]);
            } else {
                return sorted[base];
            }
        };
    }

    var request = new XMLHttpRequest();
    request.addEventListener("load", reqListener.bind(this));
    request.open("GET", "http://tocoffee.azurewebsites.net/result");
    //request.open("GET", "https://localhost:5001/result");
    request.send();
}

var app = new app();