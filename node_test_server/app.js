const express = require('express');
const app = express();
const port = 3000;
const fs = require('fs');

app.use(express.static('packages'))

app.get('/package/:id', (req, res) => {
    var data = JSON.parse(fs.readFileSync("packages\\" + req.params.id + "\\manifest.json"));
    res.json(data);
});

app.listen(port, () => console.log(`Example app listening at http://localhost:${port}`));