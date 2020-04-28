function app() {
    let lessThan60Seconds = [];
    let lessThan180Seconds = [];
    let lessThan300Seconds = [];
    let lessThan600Seconds = [];
    let longLastingDeployments = [];
    let services = {};

    function reqListener(event) {
        let response = JSON.parse(event.target.response);
        let deployments = response;

        for (let i = 0; i < deployments.length; i++) {
            let deployment = deployments[i];
            services[deployment.serviceName] = deployment;
            determineBucket(deployment);
        }

        renderBuckets();

        $('.ui.label').popup();
        $('.duration-chart').popup({
            title: 'Duration',
            html: '<canvas id="durationChart" width="400" height="250"></canvas>',
            onVisible: (e) => {
                let el = document.getElementById('durationChart');
                let serviceData = services[$(e).data('service')];
                let chartData = [];
                let labels = [];

                let sorted = serviceData.metadata.sort((a, b) => {
                    return new Date(a.dateAndTime) - new Date(b.dateAndTime);
                });

                sorted.forEach((value, index) => {
                    chartData.push({
                        x: value.dateAndTime,
                        y: value.duration
                    });

                    labels.push(value.dateAndTime);
                });

                let chart = new Chart(el, {
                    type: 'line',
                    data: {
                        datasets: [{
                            label: 'Deployment duration (s)',
                            data: chartData
                        }],
                        labels: labels
                    },
                    options: {
                        responsive: false
                    }
                });
            }
        });
    }

    function determineBucket(deployment) {
        let deploymentTime = deployment.metadata[0].duration;

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

        let mainContainer = document.getElementById('main-container');
        mainContainer.classList.remove('loading');
    }

    function renderBucket(elementId, bucket) {
        var htmlElement = document.getElementById(elementId);
        for (let i = 0; i < bucket.length; i++) {
            let service = bucket[i];
            let row = document.createElement('tr');
            let minutes = Math.floor(service.metadata[0].duration / 60);
            let seconds = service.metadata[0].duration - minutes * 60;
            let html = '';
            let stability = calculateStability(service.metadata);
            let metadataPosition = service.serviceName.indexOf("(");
            let serviceName = metadataPosition !== -1 ? service.serviceName.slice(0, metadataPosition - 1) : service.serviceName;
            let typeName = metadataPosition !== -1 ? service.serviceName.slice(metadataPosition, service.serviceName.length - 1).replace("(", "") : "-";
            console.log(typeName);

            if (minutes === 0) {
                html = `<td>${serviceName}</td><td class="text-center">${renderTypeLabels(typeName)}</td><td class="text-center"><span class="duration-chart" data-service="${service.serviceName}">${seconds}s</span></td><td class="text-center">${stability}</td>`;
            } else {
                html = `<td>${serviceName}</td><td class="text-center">${renderTypeLabels(typeName)}</td><td class="text-center"><span class="duration-chart" data-service="${service.serviceName}">${minutes}m ${seconds}s</span></td><td class="text-center">${stability}</td>`;
            }

            row.innerHTML = html;
            htmlElement.appendChild(row);
        }

        function renderTypeLabels(type) {
            let types = type.split('|');
            let result = '';

            for(let i = 0; i < types.length; i++) {
                result += `<div class="ui label" data-variation="inverted">${types[i]}</div>`;
            }

            return result;
        }

        function calculateStability(metadata) {
            let q25 = quantile(metadata, .25);
            let q50 = quantile(metadata, .50);
            let q75 = quantile(metadata, .75);
            let q99 = quantile(metadata, .99);
            let percentiles = `.25: <b>${q25}s</b><br />.50: <b>${q50}s</b><br />.75: <b>${q75}s</b><br />.99: <b>${q99}s</b>`;

            let ratio = q50 / q99 * 100;
            if (ratio > 90) {
                return `<div class="ui label" data-html="${percentiles}" data-variation="inverted">Good!</div>`;
            }

            if (ratio > 50) {
                return `<div class="ui label" data-html="${percentiles}" data-variation="inverted">Average</div>`;
            }

            return `<div class="ui label" data-html="${percentiles}" data-variation="inverted">Bad :(</div>`;
        }

        function asc(arr) {
            return arr.sort((a, b) => a.duration - b.duration);
        }

        function sum(arr) {
            return arr.reduce((a, b) => a.duration + b.duration, 0);
        }

        function mean(arr) {
            return sum(arr) / arr.length;
        }

        function std(arr) {
            const mu = mean(arr);
            const diffArr = arr.map(a => (a.duration - mu) ** 2);
            return Math.sqrt(sum(diffArr) / (arr.length - 1));
        }

        function quantile(arr, q) {
            const sorted = asc(arr);
            const pos = ((sorted.length) - 1) * q;
            const base = Math.floor(pos);
            const rest = pos - base;
            if (sorted.length > 1 && (sorted[base + 1].duration !== undefined)) {
                return sorted[base].duration + rest * (sorted[base + 1].duration - sorted[base].duration);
            } else {
                return sorted[base].duration;
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