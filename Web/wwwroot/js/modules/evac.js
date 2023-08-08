
var _map, _evacDataSource;

export function initMap(subscriptionKey) {
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
}

export function renderEvacAreas(evacAreas) {
    var _evacDataSource = new atlas.source.DataSource();
    _map.sources.add(_evacDataSource);

    //Add polygon to data source.
    evacAreas.forEach(function (evacArea) {
        evacArea.boundingAreas.forEach(function (boundingArea) {
            var polygonCoordinateGroups = [];
            boundingArea.coordinates.forEach(function (coordinate) {
                polygonCoordinateGroups.push([coordinate.longitude, coordinate.latitude]);
            });
            _evacDataSource.add(new atlas.data.Polygon([polygonCoordinateGroups]));
        });
        
    });

    _map.layers.add(new atlas.layer.PolygonLayer(_evacDataSource, null, {
        fillColor: 'red',
        fillOpacity: 0.7
    }), 'labels');
}