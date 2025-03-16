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
    res.render('./dashboard.ejs', { userid: req.session.userid });
  }
  else {
    res.render('./index.ejs');
  }
});

app.post("/login/", (req, res) => {
  let userid = req.body?.userid || "";
  let password = req.body?.password || "";
  if (userid === "rbtnn" && password === "NGf.-zu!ZPhE6*nnoJ*c") {
    req.session.authenticated = true;
    req.session.userid = userid;
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
