# Cabrillo v3 QSO Data Specification

Source: [World Wide Radio Operators Foundation](https://wwrof.org/cabrillo/cabrillo-qso-data/)

## Specification

Cabrillo Specification – QSO Data

Last update: 15 March 2021

Each contact in the log is reported using the QSO tag. Some items on this line will be different for each contest depending on the exchange information.

## QSO Data High Level Examples

Two QSO Data Lines:

| QSO Data |
| ------------------------ |
| QSO:  3799 PH 1999-03-06 0711 HC8N           59 700    W1AW           59 CT     0 |
| QSO:  3799 PH 1999-03-06 0712 HC8N           59 700    N5KO           59 CA     0 |

---

WWROF QSO Entries Specification Depiction:

| tag | freq | mo | date | time | call | rst | exch | call | rst | exch | t |
| --- | ---- | -- | ---- | ---- | ---- | --- | ---- | ---- | --- | ---- | - |
| QSO: | ***** | ** | yyyy-mm-dd | nnnn | ************* | nnn | ****** | ************* | nnn | ****** | n |
| QSO: | 3799 | PH | 1999-03-06 | 0711 | HC8N | 59 | 700 | W1AW | 59 | CT | 0 |
| QSO: | 3799 | PH | 1999-03-06 | 0712 | HC8N | 59 | 700 | N5KO | 59 | CA | 0 |

---

QSO Tag Breakdown:

- Freq: "Frequency", 1-5 character string
- Mo: "Mode", 1-2 character string
- Date: `yyyy-MM-dd`
- Time: 24-hr time without delimiters, `hhmm`
- Call: 1-13 character string
- RST: "Readability Strength Tone", Integer range 2-3
- Exch: "Exchange", 1-6 character string
- Call: "Call sign", 3-13 character string with possible '/' characters
- T: "Transmitter Id", Integer range 0-1, nullable

Field Groups (for design explanation purposes):

- Info Sent: Contains 1st Call, 1st RST, and 1st EXCH of a QSO record (the Info Prefix)
- Info Rcvd: Contains 2nd Call, 2nd RST, and 2nd EXCH of a QSO record (the Info Suffix)

## QSO Tags

QSO: qso-data

Each line must begin with “QSO:” followed by a space. The QSO templates show each item at a specific column. All of the major contest sponsors will also accept the fields in any column as long as there is a space between each item.

X-QSO: qso-data
Any QSO marked with this tag will be ignored in your log. Some contests may use this information during log checking.

Explanation of Fields

freq is frequency or band:

    1800 or actual frequency in kHz
    3500 or actual frequency in kHz
    7000 or actual frequency in kHz
    14000 or actual frequency in kHz
    21000 or actual frequency in kHz
    28000 or actual frequency in kHz
    50
    70
    144
    222
    432
    902
    1.2G
    2.3G
    3.4G
    5.7G
    10G
    24G
    47G
    75G
    122G
    134G
    241G
    LIGHT

mo is mode:

        CW
        PH
        FM
        RY
        DG

(in the case of cross-mode QSOs, indicate the transmitting mode)

date is UTC date in yyyy-mm-dd form

time is UTC time in nnnn form (0000 – 2359)

call is callsign

only A-Z, 0-9 and / permitted

exch is contest exchange. Each contest will have its own exchange elements. Check with the contest sponsor for their requirements and formatting.

t is transmitter id. Used to identify RUN/MULT, RUN1/RUN2 stations in one- or two-transmitter categories (M/2, and CQWW M/S). Not used in single-op or M/M. It is a single digit of 0 or 1.
QSO Template and Log Examples

Contest sponsors are encouraged to include template and log file examples on their web page along with the rules. This will help entrants to format the log correctly for that contest.

- [CQ WW DX Contest](http://www.cqww.com/cabrillo.htm)
- [CQ WPX Contest](http://www.cqwpx.com/cabrillo.htm)
- [CQ WPX RTTY Contest](http://www.cqwpxrtty.com/cabrillo.htm)
- [CQ WW VHF Contest](http://www.cqww-vhf.com/cabrillo.htm)
- [ARRL](http://www.arrl.org/cabrillo-format-tutorial)
- [RSGB](http://www.rsgbcc.org/hf/formats/templates.shtml)
- [Russian DX Contest (RDXC)](http://www.rdxc.org/asp/pages/logtip.asp)
- [NCJ North American QSO Party](http://ncjweb.com/NAQP_Cabrillo_Template.txt)
- [New England QSO Party](https://neqp.org/cabrillo.html)
- [UBA DX Contest](http://www.uba.be/sites/default/files/uploads/hf_contests/uba_contest_cabrillo_template.pdf)
