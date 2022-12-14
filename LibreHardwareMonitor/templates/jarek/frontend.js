const url = window.location.origin;
console.log(url);
var dataNumber = 0;
var packageNumber = 0;
let dataTemp = [];
let dataLoad = [];

setInterval(async function () {
    let obj = await (await fetch("/data.json")).json()
    let cpu = obj.Children[0].Children.find(x => x.Type == "Cpu");

    let temps = cpu.Children.find(x => x.Type == "Temperature").Children;
    let loads = cpu.Children.find(x => x.Type == "Load").Children;

    let gpu = obj.Children[0].Children.find(x => ["GpuAmd", "GpuIntel", "GpuNvidia"].includes(x.Type));
    let gpuLoads = gpu.Children.find(x => x.Type == "Load").Children;
    console.log("gpuloads", gpuLoads.length);
    dataTemp = temps.filter(x => x.Type == "Temperature");
    dataLoad = loads.filter(x => x.Text == "CPU Total");
    dataGpuLoad = gpuLoads.filter(x => x.Text == "GPU Core");
    const cpuload = dataLoad[0].Value.slice(0, -1);
    const gpuload = dataGpuLoad[0].Value.slice(0, -1);
    console.log("cpuload", cpuload);
    cpuLoadChart.data.datasets[0].data = [100 - cpuload, cpuload];
    cpuLoadChart.update();

    gpuLoadChart.data.datasets[0].data = [100 - gpuload, gpuload];
    gpuLoadChart.update();
}, 1200);

//chart
var chartColors = {
    red: "rgb(255, 99, 132)",
    orange: "rgb(255, 159, 64)",
    yellow: "rgb(255, 205, 86)",
    green: "rgb(75, 192, 192)",
    blue: "rgb(54, 162, 235)",
    purple: "rgb(153, 102, 255)",
    grey: "rgb(201, 203, 207)"
};

var chartColorsArr = [
    "rgb(255, 99, 132)",
    "rgb(255, 159, 64)",
    "rgb(255, 205, 86)",
    "rgb(75, 192, 192)",
    "rgb(54, 162, 235)",
    "rgb(153, 102, 255)",
    "rgb(201, 203, 207)"
]

function onRefresh(chart) {
    if (chart.ctx == cpuTempCtx) {
        dataTemp.forEach(function (tempNode) {
            let dataset = chart.config.data.datasets.find(x => x.id == tempNode.id);
            if (!dataset) {
                dataset = {
                    id: tempNode.id,
                    label: tempNode.Text,
                    borderColor: chartColorsArr[Math.floor(Math.random() * chartColorsArr.length)],
                    borderWidth: 1,
                    borderCapStyle: 'butt',
                    fill: false,
                    pointHoverBorderWidth: 0,
                    pointRadius: 1,
                    pointHitRadius: 1,
                    lineTension: 0.0,
                    data: []
                }
                chart.config.data.datasets.push(dataset);
            }
            dataset.data.push({
                x: new Date(),
                y: tempNode.Value.slice(0, -2)
            });
        });

        if (chart.ctx == cpuLoadChart) {
            dataLoad.forEach(function (tempNode) {
                let dataset = chart.config.data.datasets.find(x => x.id == tempNode.id);
                dataset.data = [100 - tempNode.Value, tempNode.Value];
            });
        }
    }
}

var color = Chart.helpers.color;
var config = {
    type: "line",
    data: {
        datasets: [

        ]
    },
    options: {
        plugins: {
            legend: {
                display: false,
                labels: {
                    color: 'rgb(255, 99, 132)'
                }
            }
        },
        legend: {
            display: false
        },
        title: {
            display: false,
            text: "CPU temp"
        },
        scales: {
            xAxes: [
                {
                    type: "realtime",
                    realtime: {
                        duration: 20000,
                        refresh: 1000,
                        delay: 100,
                        onRefresh: onRefresh,
                        displayFormats: {
                            hour: "hA"
                        }
                    },
                    time: {
                        displayFormats: {
                            hour: "hA"
                        }
                    },
                    scaleLabel: {
                        display: false,
                        labelString: "Time"
                    }
                }
            ],
            yAxes: [
                {
                    ticks: {
                        max: 105,
                        min: 20
                    },
                    scaleLabel: {
                        display: false,
                        labelString: "Temperature"
                    }
                }
            ]
        },
        tooltips: {
            mode: "nearest",
            intersect: false
        },
        hover: {
            mode: "nearest",
            intersect: false
        }
    }
};

var cpuLoadconfig = {
    options: {
        title: {
            display: false,
            text: "CPU Load"
        },
        legend: {
            display: false
        }
    },
    type: 'doughnut',
    data: [],
    data: {
        // labels: ['Free', 'Load'],
        datasets: [{
            data: [],
            backgroundColor: [
                'green',
                'red'
            ],
            borderColor: [
                'green',
                'red'
            ],
            borderWidth: 1
        }]
    }
};

var gpuLoadconfig = {
    options: {
        title: {
            display: false,
            text: "GPU Load"
        },
        legend: {
            display: false
        },
    },
    type: 'doughnut',
    data: [],
    data: {
        labels: ['Free', 'Load'],
        datasets: [{
            data: [],
            backgroundColor: [
                'green',
                'red'
            ],
            borderColor: [
                'green',
                'red'
            ],
            borderWidth: 1
        }]
    }
};

var cpuTempCtx = document.getElementById("cpuTempChart").getContext("2d");
var cpuLoadCtx = document.getElementById("cpuLoadChart").getContext("2d");
var gpuLoadCtx = document.getElementById("gpuLoadChart").getContext("2d");
var cpuTempChart = new Chart(cpuTempCtx, config);
var cpuLoadChart = new Chart(cpuLoadCtx, cpuLoadconfig);
var gpuLoadChart = new Chart(gpuLoadCtx, gpuLoadconfig);


$(function () {
    $('.chart').easyPieChart({
        size: 160,
        barColor: "#17d3e6",
        scaleLength: 0,
        lineWidth: 15,
        trackColor: "#373737",
        lineCap: "circle",
        animate: 1000,
        onStep: function (value) {
            //this.$el.find('span').text(~~value);
        }
    });
});

setInterval(function () {
    const val = Math.floor(Math.random() * 101);
    $(".chart").each(function () {
        $(this).data('easyPieChart').update(val);
        $(this).find('span').text(val + "%")
    });

    $(".progress").each(function () {
        $(this).progressbar({
            value: Math.floor(Math.random() * 101)
        });
    });
}, 1000);




