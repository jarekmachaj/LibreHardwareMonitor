const url = window.location.origin;
console.log(url);
var dataNumber = 0;
var packageNumber = 0;
let dataTemp = [];

setInterval(async function () {
  let obj = await (await fetch("/data.json")).json()
  let cpu = obj.Children[0].Children.find(x => x.Type == "Cpu");
  console.log("cpu", cpu);
  let temps = cpu.Children.find(x => x.Type == "Temperature").Children;
  dataTemp = temps.filter(x => x.Type == "Temperature");
}, 1000);

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
  dataTemp.forEach(function (tempNode){
    let dataset = chart.config.data.datasets.find(x => x.id == tempNode.id);
    console.log("tempNode.text", tempNode);
    if (!dataset) {
      dataset = {
        id: tempNode.id,
        label: tempNode.Text,
        // backgroundColor: color(chartColors.red).alpha(0.5).rgbString(),
        borderColor: chartColorsArr[Math.floor(Math.random()*chartColorsArr.length)],
        fill: false,
        // lineTension: 0,
        // borderDash: [8, 4],
        data: []
      }
      chart.config.data.datasets.push(dataset);
    }
    dataset.data.push({
      x: Date.now(),
      y: tempNode.Value.slice(0, -2)
    });
  });
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
      display: true,
      text: "CPU temps"
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
          },
          scaleLabel: {
            display: true,
            labelString: "Time"
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
            display: true,
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
var ctx = document.getElementById("tempChart").getContext("2d");
var chart = new Chart(ctx, config);
