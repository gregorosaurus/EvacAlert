
var _map, _evacOrdersDataSource, _evacAlertsDataSource;

export function initMap(subscriptionKey, dotnetRef) {
    _map = new atlas.Map('map-area', {
        center: [-121.55324378270137, 50.896621841510914,],
        //style: 'grayscale_dark',
        zoom: 5,
        language: 'en-US',
        authOptions: {
            authType: 'subscriptionKey',
            subscriptionKey: subscriptionKey
        }
    });

    _map.events.add('click', (e) => {
        // Get the click position
        const clickPosition = e.position;

        // You can perform global map click actions here
        // For example, displaying information or performing other tasks
        console.log('Map Clicked at:', clickPosition);

        dotnetRef.invokeMethodAsync('MapWasClicked', e.position)
        .then(data => {
            console.log(data);
        });
    });
}

export function drawFacilities(facilities) {
    var dataSource = new atlas.source.DataSource();
    _map.sources.add(dataSource);
    facilities.forEach(function (facility) {
        var point = new atlas.Shape(new atlas.data.Point([facility.longitude, facility.latitude]));
        //Add the symbol to the data source.
        dataSource.add([point]);
    });
    

    //Create a symbol layer using the data source and add it to the map
    _map.layers.add(new atlas.layer.SymbolLayer(dataSource, null));
}

export function renderRegions(regions) {
    var regionsDataSource = new atlas.source.DataSource();
    _map.sources.add(regionsDataSource);

    regions.forEach(function (region) {
        region.boundingAreas.forEach(function (boundingArea) {
            var polygonCoordinateGroups = [];
            boundingArea.coordinates.forEach(function (coordinate) {
                polygonCoordinateGroups.push([coordinate.longitude, coordinate.latitude]);
            });

            regionsDataSource.add(new atlas.Shape(new atlas.data.Feature(
                new atlas.data.Polygon([
                    polygonCoordinateGroups
                ])
            )));

            /*Create and add a polygon layer to render the polygon to the map*/
            _map.layers.add(new atlas.layer.LineLayer(regionsDataSource, null, {
                strokeColor: 'black',
                strokeWidth: 3,
                //strokeDashArray: [8, 2] // Dash pattern (10 pixels on, 5 pixels off)
            }), 'labels')            
        });
    });


    _map.layers.add(new atlas.layer.PolygonLayer(regionsDataSource, null, {
        fillOpacity:0,
        strokeColor: 'black', // Border color
        strokeThickness: 2, // Border thickness
        strokeDashArray: [10, 5] // Dash pattern (10 pixels on, 5 pixels off)
    }), 'labels');
}

export function renderEvacAreas(evacAreas) {
    var _evacOrdersDataSource = new atlas.source.DataSource();
    _map.sources.add(_evacOrdersDataSource);

    var _evacAlertsDataSource = new atlas.source.DataSource();
    _map.sources.add(_evacAlertsDataSource);


    //Add polygon to data source.
    evacAreas.forEach(function (evacArea) {
        if (evacArea.orderStatus == 'Order') {
            evacArea.boundingAreas.forEach(function (boundingArea) {
                var polygonCoordinateGroups = [];
                boundingArea.coordinates.forEach(function (coordinate) {
                    polygonCoordinateGroups.push([coordinate.longitude, coordinate.latitude]);
                });
                _evacOrdersDataSource.add(new atlas.data.Polygon([polygonCoordinateGroups]));
            });
        } else {
            evacArea.boundingAreas.forEach(function (boundingArea) {
                var polygonCoordinateGroups = [];
                boundingArea.coordinates.forEach(function (coordinate) {
                    polygonCoordinateGroups.push([coordinate.longitude, coordinate.latitude]);
                });
                _evacAlertsDataSource.add(new atlas.data.Polygon([polygonCoordinateGroups]));
            });
        }
    });

    _map.layers.add(new atlas.layer.PolygonLayer(_evacOrdersDataSource, null, {
        fillColor: 'red',
        fillOpacity: 0.45
    }), 'labels');

    _map.layers.add(new atlas.layer.PolygonLayer(_evacAlertsDataSource, null, {
        fillColor: 'orange',
        fillOpacity: 0.45
    }), 'labels');
}