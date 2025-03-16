const express = require("express");
const cors = require("cors");
const app = express();
const session = require('express-session');

app.use(cors());
app.set("view engine", "ejs");
app.set("trust proxy", 1);
app.use(express.json());
app.use(express.urlencoded({
  extended: true
}));
app.use(express.static('public'));
app.use(session({
    secret: 'your_secret_key',
    resave: false,
    saveUninitialized: true,
    cookie: {
        secure: true,
        httpOnly: true,
        sameSite: 'strict',
        maxAge: 31 * 24 * 60 * 60 * 1000,
    }
}));

app.get("/", (req, res) => {
  if (req.session.authenticated) {
    res.render('./dashboard.ejs', { username: req.session.username });
  }
  else {
    res.render('./index.ejs');
  }
});

app.post("/login/", (req, res) => {
  let username = req.body?.username || "";
  let password = req.body?.password || "";
  if (username === "rbtnn" && password === "NGf.-zu!ZPhE6*nnoJ*c") {
    req.session.authenticated = true;
    req.session.username = username;
  }
  res.redirect('/');
});

app.get("/logout/", (req, res) => {
  req.session.authenticated = false;
  res.redirect('/');
});

app.listen(80, () => {
    console.log("Start server on port 80.");
});
