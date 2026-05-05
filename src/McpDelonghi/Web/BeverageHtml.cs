namespace McpDelonghi.Web;

internal static class BeverageHtml
{
    public static readonly string Page = """
        <!DOCTYPE html>
        <html lang="fr">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>De'Longhi — Barista App</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
            body {
              background: #180C05;
              color: #FBF0DD;
              font-family: Georgia, "Times New Roman", serif;
              min-height: 100vh;
              display: flex;
              align-items: center;
              justify-content: center;
              padding: 1.5rem;
            }
            .card {
              background: #341A0A;
              border: 1px solid #C64018;
              border-top: 4px solid #C64018;
              border-radius: 6px;
              padding: 2.4rem 2rem 2rem;
              width: 460px;
              max-width: 100%;
              box-shadow: 0 8px 40px #0009;
            }
            .logo {
              font-size: 1.0rem;
              letter-spacing: 0.18em;
              text-transform: uppercase;
              color: #D49A14;
              margin-bottom: 0.3rem;
            }
            h1 {
              font-size: 2rem;
              color: #FBF0DD;
              margin-bottom: 0.2rem;
              line-height: 1.15;
            }
            .subtitle {
              color: #9A7050;
              font-style: italic;
              font-size: 0.9rem;
              margin-bottom: 1.6rem;
            }
            .status-badge {
              display: inline-flex;
              align-items: center;
              gap: 0.5rem;
              background: #281508;
              border: 1px solid #4A2A14;
              border-radius: 20px;
              padding: 0.3rem 0.85rem;
              font-size: 0.8rem;
              color: #9A7050;
              margin-bottom: 1.6rem;
              transition: all 0.3s;
            }
            .status-badge.ok  { border-color: #2A8A40; color: #6DCF6D; background: #0C1F0A; }
            .status-badge.err { border-color: #C64018; color: #F09070; background: #1F0A05; }
            .status-dot {
              width: 7px; height: 7px;
              border-radius: 50%;
              background: currentColor;
              flex-shrink: 0;
            }
            label {
              display: block;
              font-size: 0.75rem;
              color: #9A7050;
              text-transform: uppercase;
              letter-spacing: 0.1em;
              margin-bottom: 0.4rem;
            }
            select, input[type="number"] {
              width: 100%;
              background: #1E0C04;
              border: 1px solid #4A2A14;
              border-radius: 4px;
              color: #FBF0DD;
              padding: 0.65rem 0.85rem;
              font-family: Georgia, serif;
              font-size: 0.95rem;
              margin-bottom: 1.1rem;
              -webkit-appearance: none;
              appearance: none;
              transition: border-color 0.2s;
            }
            select { background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%239A7050' stroke-width='1.5' fill='none' stroke-linecap='round'/%3E%3C/svg%3E"); background-repeat: no-repeat; background-position: right 0.8rem center; padding-right: 2.2rem; }
            select:focus, input:focus {
              outline: none;
              border-color: #D49A14;
            }
            .row { display: flex; gap: 0.9rem; }
            .row > .field { flex: 1; min-width: 0; }
            button[type="submit"] {
              width: 100%;
              background: #C64018;
              color: #FBF0DD;
              border: none;
              border-radius: 4px;
              padding: 0.85rem 1rem;
              font-family: Georgia, serif;
              font-size: 1.05rem;
              cursor: pointer;
              transition: background 0.2s, transform 0.1s;
              margin-top: 0.3rem;
              letter-spacing: 0.04em;
            }
            button[type="submit"]:hover:not(:disabled) { background: #D45828; transform: translateY(-1px); }
            button[type="submit"]:active:not(:disabled) { transform: translateY(0); }
            button[type="submit"]:disabled { background: #3A1A0A; color: #6A4030; cursor: not-allowed; }
            #result {
              margin-top: 1.1rem;
              padding: 0.85rem 1rem;
              border-radius: 4px;
              font-size: 0.9rem;
              display: none;
              line-height: 1.5;
            }
            #result.ok  { background: #0C1F0A; border: 1px solid #2A8A40; color: #90E880; }
            #result.err { background: #1F0A05; border: 1px solid #C64018; color: #F09070; }
            .divider {
              border: none;
              border-top: 1px solid #3A1A0A;
              margin: 1.4rem 0 1.2rem;
            }
            input[type="number"]::-webkit-inner-spin-button { -webkit-appearance: none; }
          </style>
        </head>
        <body>
          <div class="card">
            <div class="logo">De'Longhi · MCP App</div>
            <h1>Barista</h1>
            <div class="subtitle">Commandez votre café via le serveur MCP</div>

            <div class="status-badge" id="status-badge">
              <span class="status-dot"></span>
              <span id="status-text">Connexion à la machine…</span>
            </div>

            <form id="brew-form" autocomplete="off">
              <label for="beverage">Recette</label>
              <select id="beverage" name="beverage" required disabled>
                <option value="">— chargement —</option>
              </select>

              <div class="row">
                <div class="field">
                  <label for="profile">Profil utilisateur</label>
                  <select id="profile" name="profile">
                    <option value="1">Profil 1</option>
                    <option value="2" selected>Profil 2</option>
                    <option value="3">Profil 3</option>
                  </select>
                </div>
                <div class="field">
                  <label for="qty">Volume en mL — optionnel</label>
                  <input type="number" id="qty" name="qty" min="10" max="600" placeholder="par défaut recette">
                </div>
              </div>

              <button type="submit" id="brew-btn" disabled>Préparer le café</button>
            </form>

            <div id="result"></div>
          </div>

          <script>
            const form      = document.getElementById('brew-form');
            const selBev    = document.getElementById('beverage');
            const brewBtn   = document.getElementById('brew-btn');
            const badge     = document.getElementById('status-badge');
            const badgeText = document.getElementById('status-text');
            const result    = document.getElementById('result');

            async function loadBeverages() {
              try {
                const res = await fetch('/api/beverages');
                if (!res.ok) throw new Error('HTTP ' + res.status);
                const list = await res.json();
                selBev.innerHTML = '<option value="">— choisir une recette —</option>';
                list.forEach(b => {
                  const o = document.createElement('option');
                  o.value = b.key;
                  o.textContent = b.name;
                  selBev.appendChild(o);
                });
                selBev.disabled = false;
                brewBtn.disabled = false;
                badge.className = 'status-badge ok';
                badgeText.textContent = list.length + ' recettes disponibles';
              } catch {
                badge.className = 'status-badge err';
                badgeText.textContent = 'Machine hors ligne';
              }
            }

            form.addEventListener('submit', async e => {
              e.preventDefault();
              const beverageKey = selBev.value;
              if (!beverageKey) return;
              const profile = parseInt(document.getElementById('profile').value);
              const qtyRaw  = document.getElementById('qty').value;
              const body = { beverageKey, profile };
              if (qtyRaw) body.quantityMl = parseInt(qtyRaw);

              brewBtn.disabled = true;
              brewBtn.textContent = 'Préparation en cours…';
              result.style.display = 'none';

              try {
                const res  = await fetch('/api/brew', {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify(body)
                });
                const data = await res.json();
                result.className = res.ok ? 'ok' : 'err';
                result.textContent = res.ok ? data.message : data.error;
                result.style.display = 'block';
              } catch {
                result.className = 'err';
                result.textContent = 'Erreur de connexion avec le serveur MCP.';
                result.style.display = 'block';
              } finally {
                brewBtn.disabled = false;
                brewBtn.textContent = 'Préparer le café';
              }
            });

            loadBeverages();
          </script>
        </body>
        </html>
        """;
}
