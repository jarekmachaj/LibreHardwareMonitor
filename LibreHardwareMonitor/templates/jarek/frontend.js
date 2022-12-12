const url = window.location.origin;
console.log(url);
// let socket = io();
// let socket = io.connect(url);
// console.log(socket);
var datap = document.querySelector("#data");
var dataNumber = 0;
var packageNumber = 0;

setInterval(async function () {
  let obj = await (await fetch("http://localhost:8085/data.json")).json()
  let data = obj.Children[0].Children.find(x => x.id == 42).Children.find(x => x.id == 67).Children.find(x => x.id == 86).Value;
  let package = obj.Children[0].Children.find(x => x.id == 42).Children.find(x => x.id == 67).Children.find(x => x.id == 76).Value;
  data = data.slice(0, -2);
  package = package.slice(0, -2);
  console.log(data);
  datap.textContent = data;
  dataNumber = Number(data);
  packageNumber = Number(package);
}, 1000);

// socket.on("mqtt", );

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

function onRefresh(chart) {
  chart.config.data.datasets.forEach(function (dataset) {
    let data = {};
    if (dataset.id == "general") {
      data = {
        x: Date.now(),
        y: dataNumber
      }
    }    

    if (dataset.id == "package") {
      data = {
        x: Date.now(),
        y: packageNumber
      }
    }
    
    dataset.data.push(data);
  });
}

var color = Chart.helpers.color;
var config = {
  type: "line",
  data: {
    datasets: [
      {
        id: "general",
        label: "core",
        backgroundColor: color(chartColors.red).alpha(0.5).rgbString(),
        borderColor: chartColors.blue,
        fill: false,
        lineTension: 0,
        borderDash: [8, 4],
        data: []
      },
      {
        id: "package",
        label: "package",
        backgroundColor: color(chartColors.orange).alpha(0.5).rgbString(),
        borderColor: chartColors.orange,
        fill: false,
        lineTension: 0,
        borderDash: [8, 4],
        data: []
      }
    ]
  },
  options: {
    title: {
      display: false,
      text: "tytul legendae"
    },
    scales: {
      xAxes: [
        {
          type: "realtime",
          realtime: {
            duration: 20000,
            refresh: 1000,
            delay: 100,
            onRefresh: onRefresh
          }
        }
      ],
      yAxes: [
        {
          ticks: {
            max: 120,
            min: 0
          },
          scaleLabel: {
            display: false,
            labelString: "value"
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
var ctx = document.getElementById("myChart").getContext("2d");
var chart = new Chart(ctx, config);
