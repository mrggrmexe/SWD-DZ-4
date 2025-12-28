import { useEffect, useState } from "react";
import { api, getUserId, setUserId } from "./api";

type Order = {
  orderId: string;
  amountMinor: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  description?: string | null;
};

export default function App() {
  const [userId, setUserIdState] = useState(getUserId());
  const [topUpAmount, setTopUpAmount] = useState(5000);
  const [orderAmount, setOrderAmount] = useState(1000);
  const [orders, setOrders] = useState<Order[]>([]);
  const [log, setLog] = useState<string>("");

  const write = (s: string) => setLog(prev => `${new Date().toISOString()}  ${s}\n${prev}`);

  useEffect(() => {
    setUserIdState(getUserId());
  }, []);

  async function saveUser() {
    setUserId(userId);
    write(`UserId saved: ${userId}`);
  }

  async function createAccount() {
    await api<void>("/accounts", { method: "POST" });
    write("Account created (or already exists).");
  }

  async function topUp() {
    await api<void>("/accounts/topup", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ amountMinor: topUpAmount })
    });
    write(`TopUp OK: ${topUpAmount}`);
  }

  async function createOrder() {
    const r = await api<{ orderId: string; status: string }>("/orders", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ amountMinor: orderAmount, description: "from frontend" })
    });
    write(`Order created: ${r.orderId} status=${r.status}`);
  }

  async function loadOrders() {
    const list = await api<Order[]>("/orders");
    setOrders(list);
    write(`Orders loaded: ${list.length}`);
  }

  return (
    <div className="page">
      <header className="header">
        <div>
          <h1>SWD DZ-4 Frontend</h1>
          <p className="muted">UI для ручной проверки Orders/Payments через ApiGateway</p>
        </div>
      </header>

      <section className="card">
        <div className="cardTitle">User</div>
        <div className="row">
          <input
            className="input"
            value={userId}
            onChange={e => setUserIdState(e.target.value)}
            placeholder="X-User-Id"
          />
          <button className="btn" onClick={saveUser}>Save</button>
        </div>
      </section>

      <div className="grid2">
        <section className="card">
          <div className="cardTitle">Payments</div>
          <div className="row wrap">
            <button className="btn" onClick={createAccount}>Create account</button>
            <input
              className="input small"
              type="number"
              value={topUpAmount}
              onChange={e => setTopUpAmount(Number(e.target.value))}
            />
            <button className="btn" onClick={topUp}>TopUp</button>
          </div>
        </section>

        <section className="card">
          <div className="cardTitle">Orders</div>
          <div className="row wrap">
            <input
              className="input small"
              type="number"
              value={orderAmount}
              onChange={e => setOrderAmount(Number(e.target.value))}
            />
            <button className="btn" onClick={createOrder}>Create order</button>
            <button className="btn" onClick={loadOrders}>Load orders</button>
          </div>
        </section>
      </div>

      <section className="card">
        <div className="cardTitle">Orders list</div>
        {orders.length === 0 ? (
          <div className="muted">No orders loaded.</div>
        ) : (
          <ul className="list">
            {orders.map(o => (
              <li key={o.orderId}>
                <b>{o.status}</b> — {o.amountMinor} — <span className="mono">{o.orderId}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="card">
        <div className="cardTitle">Log</div>
        <pre className="log">{log}</pre>
      </section>

      <footer className="footer">
        <span className="muted">Frontend served by Nginx in Docker</span>
      </footer>
    </div>
  );
}
